using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;

namespace VersionOne.Provisioning.LDAP
{
    public class LDAPReader
    {
        public IList<LDAPUser> GetUsersFromLdap(string serverpath, string groupDN, string username, string pwd)
        {
            string serverPath = serverpath;
            string grpDN = groupDN;
            string userName = username;
            string password = pwd;
            string fullPath = @"LDAP://" + serverPath + @"/" + grpDN;

            try
            {
                IList<LDAPUser> ldapUsersList = new List<LDAPUser>();
                
                //Connect to the LDAP store. (For local testing with VPN connected, works without explicitly passing in username & password) 
                DirectoryEntry group = new DirectoryEntry(fullPath);
                //DirectoryEntry group = new DirectoryEntry(fullPath, username, password);

                //Get the users in the group.
                DirectorySearcher ldapSearcher = GetLdapSearcher(group, "user");

                //Put the Ldap users into a "non-LDAP-aware" collection. 
                SerializeLdapUsers(ldapSearcher, ldapUsersList);

                return ldapUsersList;
            }

            catch (Exception ex)
            {
               throw ex;
            }
        }

        private DirectorySearcher GetLdapSearcher(DirectoryEntry group, string objectClass)
        {
            DirectorySearcher ldapSearcher = new System.DirectoryServices.DirectorySearcher(group);
            ldapSearcher.Filter = (string.Format("(objectClass={0})", objectClass));
            return ldapSearcher;
        }

        private void SerializeLdapUsers(DirectorySearcher ldapSearcher, IList<LDAPUser> ldapUsersList)
        {
            foreach (SearchResult ldapUser in ldapSearcher.FindAll())
            {
                DirectoryEntry de = ldapUser.GetDirectoryEntry();
                LDAPUser user = new LDAPUser();

                user.Username = de.Properties["userPrincipalName"].Value.ToString(); //username
                user.FullName = de.Properties["name"].Value.ToString(); //fullname
                user.Email = de.Properties["mail"].Value.ToString(); //email
                user.Nickname = de.Properties["sAMAccountName"].Value.ToString(); //nickname

                ldapUsersList.Add(user);
            }
        }
    }
}
