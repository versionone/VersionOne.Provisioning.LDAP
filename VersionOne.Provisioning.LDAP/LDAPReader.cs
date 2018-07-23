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
        private bool useNestedGrouping;

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
            useNestedGrouping = Convert.ToBoolean(appSettings["NestedGrouping"]);
        }

        public IList<DirectoryUser> GetUsers() {
            IList<DirectoryUser> ldapUsersList = new List<DirectoryUser>();
            IList<string> memberPaths = GetMemberPaths(fullPath);

            foreach (string memberPath in memberPaths) {
                try {
                    DirectoryUser user = new DirectoryUser();
                    using (DirectoryEntry member = GetMember(memberPath)) {
                        SetUsername(user, member);
                        SetFullName(user, member);
                        SetEmail(user, member);
                        SetNickname(user, member);
                    }
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
                DirectoryEntry entry = MakeDirectoryEntry(fullPath);
                entry.RefreshCache(propertiesToLoad);
                return entry;
            } catch (Exception ex) {
                logger.ErrorException("Unable access ldap, path: " + fullPath + "', username: '" + username + "', use default credentials: '" + useDefaultCredentials + "'", ex);
                throw;
            }
        }

        private DirectoryEntry MakeDirectoryEntry(string fullPath) {
            DirectoryEntry entry;
            if (useDefaultCredentials)
            {
                entry = new DirectoryEntry(fullPath);
            }
            else
            {
                entry = new DirectoryEntry(fullPath, username, password);
            }
            entry.AuthenticationType = AuthenticationTypes.ServerBind;
            return entry;
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
                return GetDirectoryEntry(memberPath, new[] { mapUsername, mapFullname, mapEmail, mapNickname });
            } catch (Exception ex) {
                logger.ErrorException("Unable to get directory entry for member, path: " + memberPath + ", username: " + username, ex);
                throw;
            }
        }

        private IList<string> GetMemberPaths(string userGroupPath)
        {
            // retrieve distinguished names of user members of the group
            IList<string> userMemberPaths = GetPathsOfGroupMembers(userGroupPath, "person");

            if (useNestedGrouping)
            {
                // retrieve distinguished names of group members of the group
                IList<string> groupMemberPaths = GetPathsOfGroupMembers(userGroupPath, "group");

                // should user group also have groups within it, recursively get user members of the group
                foreach (string groupMemberPath in groupMemberPaths)
                {
                    if (groupMemberPath.Equals(userGroupPath, StringComparison.OrdinalIgnoreCase)) continue;

                    // recursion here
                    foreach (string userMemberPath in GetMemberPaths(groupMemberPath))
                    {
                        // add users of nested group
                        userMemberPaths.Add(userMemberPath);
                    }
                }
            }

            return userMemberPaths;
        }

        private IList<string> GetPathsOfGroupMembers(string userGroupPath, string objClass)
        {
            DirectoryEntry searchRoot = MakeDirectoryEntry(userGroupPath);
            DirectorySearcher ds = new DirectorySearcher(searchRoot);
            ds.Filter = String.Format("(objectClass={0})", objClass );

            IList<string> groupMemberPaths = new List<string>();
            try
            {
                SearchResultCollection dsAll = ds.FindAll();
                foreach (SearchResult sr in dsAll)
                {
                    string memberPath = sr.Path;
                    groupMemberPaths.Add(memberPath);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Unable to read members from ldap, path: " + userGroupPath + ", objectClass: " + objClass, ex);
                throw;
            }
            finally { searchRoot.Dispose(); }

            return groupMemberPaths;
        }

    }
}