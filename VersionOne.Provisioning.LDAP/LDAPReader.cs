using System;
using System.Collections.Generic;
using System.DirectoryServices;
using NLog;

namespace VersionOne.Provisioning.LDAP
{
    public class LDAPReader
    {
        private readonly string groupMemberAttribute;
        private readonly string username;
        private readonly string password;
        private readonly string mapUsername;
        private readonly string mapFullname;
        private readonly string mapEmail;
        private readonly string mapNickname;
        private readonly bool useDefaultCredentials;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string root;

        public LDAPReader(string serverpath, string groupMemberAttribute, string username, string password, string mapUsername, string mapFullname, string mapEmail, string mapNickname, bool useDefaultCredentials)
        {
            root = @"LDAP://" + serverpath + @"/";
            this.groupMemberAttribute = groupMemberAttribute;
            this.username = username;
            this.password = password;
            this.mapUsername = mapUsername;
            this.mapFullname = mapFullname;
            this.mapEmail = mapEmail;
            this.mapNickname = mapNickname;
            this.useDefaultCredentials = useDefaultCredentials;
        }

        public IList<LDAPUser> GetUsersFromLdap(string groupDN)
        {

            string fullPath = root + groupDN;

            IList<LDAPUser> ldapUsersList = new List<LDAPUser>();


            DirectoryEntry group = GetDirectoryEntry(fullPath, new [] {groupMemberAttribute});
            PropertyValueCollection memberPaths = GetMemberPaths(group);

            foreach (string memberPath in memberPaths)
            {
                try
                {
                    LDAPUser user = new LDAPUser();
                    DirectoryEntry member = GetMember(memberPath);
                    SetUsername(user, member);
                    SetFullName(user, member);
                    SetEmail(user, member);
                    SetNickname(user, member);
                    ldapUsersList.Add(user);
                } catch(Exception ex)
                {
                    logger.ErrorException("Unable to read member from ldap, path: " + memberPath, ex);
                }
            }
            if(memberPaths.Count < 1)
            {
                logger.Warn("No members were returned from group: " + fullPath + ", group member attribute name: '" + groupMemberAttribute);
            }
            return ldapUsersList;
        }

        private DirectoryEntry GetDirectoryEntry(string fullPath, string[] propertiesToLoad)
        {
            try
            {
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
                entry.RefreshCache(propertiesToLoad);
                return entry;
            } catch(Exception ex)
            {
                logger.ErrorException("Unable access ldap, path: " + fullPath + "', username: '" + username + "', use default credentials: '" + useDefaultCredentials + "'", ex);
                throw;
            }
        }

        private void SetNickname(LDAPUser user, DirectoryEntry member)
        {
            try{
            user.Nickname = member.Properties[mapNickname][0].ToString(); //nickname
            }
            catch (Exception ex)
            {
                logger.ErrorException("Unable to get nickname property for member: " + member.Path + ", nickname property name: " + mapNickname, ex);
                throw;
            }
        }

        private void SetEmail(LDAPUser user, DirectoryEntry member)
        {
            try
            {
                user.Email = member.Properties[mapEmail][0].ToString(); //email
            }
            catch (Exception ex)
            {
                logger.ErrorException("Unable to get email property for member: " + member.Path + ", email property name: " + mapEmail, ex);
                throw;
            }
        }

        private void SetFullName(LDAPUser user, DirectoryEntry member)
        {
            try{
            user.FullName = member.Properties[mapFullname][0].ToString(); //fullname
            }
            catch (Exception ex)
            {
                logger.ErrorException("Unable to get fullname property for member: " + member.Path + ", fullname property name: " + mapFullname, ex);
                throw;
            }
        }

        private void SetUsername(LDAPUser user, DirectoryEntry member)
        {
            try
            {
                user.Username = member.Properties[mapUsername][0].ToString(); //username
            } catch(Exception ex)
            {
                logger.ErrorException("Unable to get username property for member: " + member.Path + ", username property name: " + mapUsername, ex);
                throw;
            }
        }

        private DirectoryEntry GetMember(string memberPath)
        {
            try
            {
                return GetDirectoryEntry(root + memberPath, new [] {mapUsername, mapFullname, mapEmail, mapNickname});
            }catch(Exception ex)
            {
                logger.ErrorException("Unable to get directory entry for member, path: " + root + memberPath + ", username: " + username, ex);
                throw;
            }
        }

        private PropertyValueCollection GetMemberPaths(DirectoryEntry group)
        {
            try
            {
                return group.Properties[groupMemberAttribute];
            }catch(Exception ex)
            {
                logger.ErrorException("Unable to get group member property for group: " + group.Path + ", group member property name: " + groupMemberAttribute, ex);
                throw;
            }
        }

    }
}
