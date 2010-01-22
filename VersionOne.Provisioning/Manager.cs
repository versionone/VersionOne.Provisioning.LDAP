using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning
{
    public class Manager
    {
        private IServices services;
        private IMetaModel model;
        public StringBuilder logstring;
        private string defaultRole;
        public List<User> deactivatedMembers;
        public List<User> newMembers;

        public Manager(IServices services, IMetaModel model, string defaultRole)
        {
            this.services = services;
            this.model = model;
            this.defaultRole = defaultRole;
            logstring = new StringBuilder();

            deactivatedMembers = new List<User>();
            newMembers = new List<User>();
        }

        public AssetList GetVersionOneUsers()
        {
            IAssetType memberType = model.GetAssetType(V1Constants.MEMBER);
            Query userQuery = new Query(memberType);

            IAttributeDefinition username = model.GetAttributeDefinition(V1Constants.USERNAME);
            userQuery.Selection.Add(username);

            //IAttributeDefinition isInactive = model.GetAttributeDefinition(V1Constants.ISINACTIVE);

            QueryResult result = services.Retrieve(userQuery);
            return result.Assets;
        }

        public IList<User> CompareUsers(IList<User> ldapUsers, AssetList versionOneUsers)
        {
            //The "action" List that will be returned 
            IList<User> v1ActionList = new List<User>();

            //Get just the usernames out of the V1 AssetList
            IList<string> v1Usernames = BuildV1UsernamesList(versionOneUsers);

            /*
            Loop through the LDAP List, looking for a username match in the V1 usernames List.
              - If a username in the LDAP List is not in the V1 List, we add it to the action List for creation in V1.
              - If a username is in both LDAP and V1, then no action is required. We remove the username from the V1 ist. 
            */

            AddNewUsersToActionList(ldapUsers, v1Usernames, v1ActionList);

            /*
            Now, what remain in the V1 usernames List are the users who are not in the LDAP List.
            Add them to the action List for deactivation. 
            */

            AddInactiveUsersToActionList(v1Usernames, v1ActionList);

            return v1ActionList;

        }

        private void AddInactiveUsersToActionList(IList<string> v1Usernames, IList<User> v1ActionList)
        {
           for (int i = 0; i < v1Usernames.Count; i++)
            {
                if (v1Usernames[i] != "admin")  //don't deactivate the main admin login
                {
                    User user = new User();
                    user.Username = v1Usernames[i];
                    user.Deactivate = true;
                    v1ActionList.Add(user);
                }

            }
        }

        private void AddNewUsersToActionList(IList<User> ldapUsers, IList<string> v1Usernames, IList<User> v1ActionList)
        {
            foreach (User userInLdap in ldapUsers)
            {
                if (v1Usernames.Contains(userInLdap.Username))
                {
                    v1Usernames.Remove(userInLdap.Username);    //should be removing 9, So 11-9 = 2 (admin and vijay)
                }
                else
                {
                    userInLdap.Create = true;
                    v1ActionList.Add(userInLdap);
                }
            }
        }

        private IList<string> BuildV1UsernamesList(AssetList versionOneUsers)
        {
            IList<string> v1Usernames = new List<string>();
            IAttributeDefinition usernameAttribute = model.GetAttributeDefinition(V1Constants.USERNAME);

            foreach (Asset userinV1 in versionOneUsers)
            {
                v1Usernames.Add(userinV1.GetAttribute(usernameAttribute).Value.ToString().Trim());
            }
            return v1Usernames;
        }

        public void UpdateVersionOne(IList<User> actionList)
        {
            /*
             * Takes the action list that resulted from the comparison between 
             * the LDAP user list and the V1 user list, and takes the appropriate 
             * action in V1. 
            */

            
            foreach (User user in actionList)
            {
                if (user.Deactivate == true)
                {
                    DeactivateVersionOneMember(user);
                }
                else if (user.Create == true)
                {
                    CreateNewVersionOneMember(user);
                }
                else if (user.Reactivate == true)
                {
                    ReactivateVersionOneMember(user);
                }
                else if (user.Delete == true)
                {
                    DeleteVersionOneMember(user);
                }
             }
                
            //Flush the cached messaging to the log file
            LogActionResult(logstring);

        }

        
        private void DeactivateVersionOneMember(User user)
        {
            string resultString = "";
            string uname = user.Username;

            try
            {
                Oid memberOid = GetMemberOidByUsername(uname);
                IOperation inactivateMember = model.GetOperation(V1Constants.INACTIVATE);
                Oid inactivatedOid = services.ExecuteOperation(inactivateMember, memberOid);

                user.Oid = memberOid;
                deactivatedMembers.Add(user);
                
                resultString = "Member with username '" + uname + "' has been deactivated in the VersionOne system.";
            }

            catch (Exception)
            {
                resultString = "Attempt to deactivate Member with username '" + uname + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);    
            }
            
        }

        private void CreateNewVersionOneMember(User user)
        {
            //Make the appropriate API calls to create the member, with username, fullname, nickname, email, and default role.
            //Default Role will default to Team Member if that attribute is left out.

            string username = user.Username;
            string resultString = "";

            try
            {
                IAssetType memberType = model.GetAssetType(V1Constants.MEMBER);
                Asset newMember = services.New(memberType, null);
                IAttributeDefinition usernameAttribute = memberType.GetAttributeDefinition("Username");
                IAttributeDefinition nameAttribute = memberType.GetAttributeDefinition("Name");
                IAttributeDefinition nicknameAttribute = memberType.GetAttributeDefinition("Nickname");
                IAttributeDefinition emailAttribute = memberType.GetAttributeDefinition("Email");
                IAttributeDefinition defaultRoleAttribute = memberType.GetAttributeDefinition("DefaultRole");

                newMember.SetAttributeValue(usernameAttribute, username);
                newMember.SetAttributeValue(nameAttribute, user.FullName);
                newMember.SetAttributeValue(nicknameAttribute, user.Nickname);
                newMember.SetAttributeValue(emailAttribute, user.Email);
                newMember.SetAttributeValue(defaultRoleAttribute, this.defaultRole);  

                services.Save(newMember);
                
                //Verify that the V1 user was created
                Oid newMemberOid = GetMemberOidByUsername(username);
                if (newMemberOid != null)
                {
                    user.Oid = newMemberOid;
                    newMembers.Add(user);
                    
                    resultString = "Member with username '" + username + "' has been created in the VersionOne system.";
                }
                else
                {
                    throw new Exception();
                }    
            }

            catch (Exception)
            {
                resultString = "Attempt to create Member with username '" + username + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);    
            }
        }

        private Oid GetMemberOidByUsername(string username)
        {
            IAssetType memberType = model.GetAssetType(V1Constants.MEMBER);
            Query userQuery = new Query(memberType);
            IAttributeDefinition usernameAttribute = memberType.GetAttributeDefinition("Username");
            userQuery.Selection.Add(usernameAttribute);

            FilterTerm term = new FilterTerm(usernameAttribute);
            term.Equal(username);
            userQuery.Filter = term;

            QueryResult result = services.Retrieve(userQuery);
            if (result.Assets.Count > 0)
            {
                return result.Assets[0].Oid;
            }
            else
            {
                return null;
            }
           }

        public void ReactivateVersionOneMember(User deactivatedUser)
        {
            string resultString = "";
            string uname = deactivatedUser.Username;

            try
            {
                Oid memberOid = GetMemberOidByUsername(uname);

                IOperation reactivateMember = model.GetOperation(V1Constants.REACTIVATE);
                Oid inactivatedOid = services.ExecuteOperation(reactivateMember, memberOid);

                resultString = "Member with username '" + uname + "' has been reactivated in the VersionOne system.";
            }

            catch (Exception)
            {
                resultString = "Attempt to reactivate Member with username '" + uname + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);
            }

        }

        public void DeleteVersionOneMember(User userToDelete)
        {
            string resultString = "";
            string uname = userToDelete.Username;

            try
            {
                Oid memberOid = GetMemberOidByUsername(uname);

                IOperation deleteMember = model.GetOperation(V1Constants.DELETE);
                Oid deletedOid = services.ExecuteOperation(deleteMember, memberOid);

                resultString = "Member with username '" + uname + "' has been deleted in the VersionOne system.";
            }

            catch (Exception)
            {
                resultString = "Attempt to delete Member with username '" + uname + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);
            }

        }

        public void LogActionResult(StringBuilder message)
        {
            /* REFACTOR THIS INTO TO A "MESSAGING AND LOGGING" CLASS(?) */
            string textToWrite = message.ToString();
            string path = @"C:\testlogs\samplelog.txt"; //NEEDS TO BE DYNAMIC -- HARD-CODED FOR TESTING ONLY.
            FileStream fstream = new FileStream(path, FileMode.Append, FileAccess.Write);
            StreamWriter logger = new StreamWriter(fstream);

            logger.Write(textToWrite);
            message.Remove(0, message.Length);
            logger.Close();
        }
        
    }
}
