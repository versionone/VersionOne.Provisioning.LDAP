/*(c) Copyright 2011, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using NLog;
using VersionOne.Provisioning;

namespace VersionOne.Provisioning.LDAP {
    public class LDAPReader : IUserDirectoryReader {
        private string groupMemberAttribute;
        private string username;
        private string password;
        private string mapUsername;
        private string mapFullname;
        private string mapEmail;
        private string mapNickname;
        private bool useDefaultCredentials;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string root;
        private string groupDN;
        private string fullPath;
        private bool useIntegratedAuth;
        private string domain;

        public void Initialize(NameValueCollection appSettings) {
            if (appSettings["useDefaultLDAPCredentials"].Trim().ToUpper() != "FALSE") {
                useDefaultCredentials = true;
            }

            root = @"LDAP://" + appSettings["ldapServerPath"] + @"/";
            groupMemberAttribute = appSettings["ldapGroupMemberAttribute"];
            username = appSettings["ldapUsername"];
            password = appSettings["ldapPassword"];
            mapUsername = appSettings["mapToV1Username"];
            mapFullname = appSettings["mapToV1Fullname"];
            mapEmail = appSettings["mapToV1Email"];
            mapNickname = appSettings["mapToV1Nickname"];
            groupDN = appSettings["ldapGroupDN"];
            fullPath = root + groupDN;
            useIntegratedAuth = Convert.ToBoolean(appSettings["IntegratedAuth"]);
            domain = appSettings["ldapDomain"];
        }

        public IList<DirectoryUser> GetUsers() {
            IList<DirectoryUser> ldapUsersList = new List<DirectoryUser>();
            DirectoryEntry group = GetDirectoryEntry(fullPath, new[] { groupMemberAttribute });
            PropertyValueCollection memberPaths = GetMemberPaths(group);

            foreach (string memberPath in memberPaths) {
                try {
                    DirectoryUser user = new DirectoryUser();
                    DirectoryEntry member = GetMember(memberPath);
                    SetUsername(user, member);
                    SetFullName(user, member);
                    SetEmail(user, member);
                    SetNickname(user, member);
                    ldapUsersList.Add(user);
                } catch (Exception ex) {
                    logger.ErrorException("Unable to read member from ldap, path: " + memberPath, ex);
                }
            }

            if (memberPaths.Count < 1) {
                logger.Warn("No members were returned from group: " + fullPath + ", group member attribute name: '" + groupMemberAttribute);
            }
            
            return ldapUsersList;
        }

        private DirectoryEntry GetDirectoryEntry(string fullPath, string[] propertiesToLoad) {
            try {
                DirectoryEntry entry;
                if (useDefaultCredentials) {
                    entry = new DirectoryEntry(fullPath);
                } else {
                    entry = new DirectoryEntry(fullPath, username, password);
                }
                entry.AuthenticationType = AuthenticationTypes.ServerBind;
                entry.RefreshCache(propertiesToLoad);
                return entry;
            } catch (Exception ex) {
                logger.ErrorException("Unable access ldap, path: " + fullPath + "', username: '" + username + "', use default credentials: '" + useDefaultCredentials + "'", ex);
                throw;
            }
        }

        private void SetNickname(DirectoryUser user, DirectoryEntry member) {
            try {
                user.Nickname = member.Properties[mapNickname][0].ToString(); //nickname
            } catch (Exception ex) {
                logger.ErrorException("Unable to get nickname property for member: " + member.Path + ", nickname property name: " + mapNickname, ex);
                throw;
            }
        }

        private void SetEmail(DirectoryUser user, DirectoryEntry member) {
            try {
                user.Email = member.Properties[mapEmail][0].ToString(); //email
            } catch (Exception ex) {
                logger.ErrorException("Unable to get email property for member: " + member.Path + ", email property name: " + mapEmail, ex);
                throw;
            }
        }

        private void SetFullName(DirectoryUser user, DirectoryEntry member) {
            try {
                user.FullName = member.Properties[mapFullname][0].ToString(); //fullname
            } catch (Exception ex) {
                logger.ErrorException("Unable to get fullname property for member: " + member.Path + ", fullname property name: " + mapFullname, ex);
                throw;
            }
        }

        private void SetUsername(DirectoryUser user, DirectoryEntry member) {
            try
            {
                string directoryUsername = member.Properties[mapUsername][0].ToString(); //username
                user.Username = useIntegratedAuth ? string.Format("{0}\\{1}", domain, directoryUsername) : directoryUsername;
            }
            catch (Exception ex) {
                logger.ErrorException("Unable to get username property for member: " + member.Path + ", username property name: " + mapUsername, ex);
                throw;
            }
        }

        private DirectoryEntry GetMember(string memberPath) {
            try {
                return GetDirectoryEntry(root + memberPath, new[] { mapUsername, mapFullname, mapEmail, mapNickname });
            } catch (Exception ex) {
                logger.ErrorException("Unable to get directory entry for member, path: " + root + memberPath + ", username: " + username, ex);
                throw;
            }
        }

        private PropertyValueCollection GetMemberPaths(DirectoryEntry group) {
            try {
                return group.Properties[groupMemberAttribute];
            } catch (Exception ex) {
                logger.ErrorException("Unable to get group member property for group: " + group.Path + ", group member property name: " + groupMemberAttribute, ex);
                throw;
            }
        }

    }
}