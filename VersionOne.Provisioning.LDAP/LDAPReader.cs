using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;

namespace VersionOne.Provisioning.LDAP
{
    public class LDAPReader
    {
        public IList<LDAPUser> GetUsersFromLdap(string serverpath, string groupDN, string username, string pwd, string mapUsername, string mapFullname, string mapEmail, string mapNickname, bool useDefaultCredentials)
        {
            string serverPath = serverpath;
            string grpDN = groupDN;
            string userName = username;
            string password = pwd;
            string root = @"LDAP://" + serverPath + @"/";
            string fullPath = root + grpDN;
            DirectoryEntry group;

            IList<LDAPUser> ldapUsersList = new List<LDAPUser>();
            
            
            if (useDefaultCredentials)
            {
                group = new DirectoryEntry(fullPath);
            }
            else
            {
                group = new DirectoryEntry(fullPath,userName,password);
            }
            
            PropertyValueCollection memberPaths = group.Properties["member"];

            foreach (string memberPath in memberPaths)
            {
                LDAPUser user = new LDAPUser();
                var member = new DirectoryEntry(root + memberPath);
                user.Username = member.InvokeGet(mapUsername).ToString(); //username
                user.FullName = member.InvokeGet(mapFullname).ToString(); //fullname
                user.Email = member.InvokeGet(mapEmail).ToString(); //email
                user.Nickname = member.InvokeGet(mapNickname).ToString(); //nickname
                ldapUsersList.Add(user);
            }

            return ldapUsersList;
            /*
                
                //Connect to the LDAP store. (For local testing with VPN connected, works without explicitly passing in username & password) 
                DirectoryEntry group = new DirectoryEntry(fullPath);
                //DirectoryEntry group = new DirectoryEntry(fullPath, username, password);

                //Get the users in the group.
                DirectorySearcher ldapSearcher = new DirectorySearcher(group)
                                                      {
                                                          Filter = (string.Format("(objectClass={0})", "user"))
                                                      };

                //Put the Ldap users into a "non-LDAP-aware" collection. 
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

                return ldapUsersList;*/
        }
    }
}
