using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.IO;
using System.DirectoryServices;
using NLog;
using VersionOne.SDK.APIClient;
using VersionOne.Provisioning.LDAP;

namespace VersionOne.Provisioning
{
    public class Manager
    {
        private IServices _services;
        private IMetaModel _model;
        public StringBuilder _logstring;
        private string _defaultRole;
        public List<User> _deactivatedMembers;
        public List<User> _newMembers;
        private bool _reactivatedUserKeepsOrigProjectAccess;
        private bool _reactivatedUserKeepsOrigDefaultRole;
        private bool _reactivatedUserKeepsOrigPassword;
        private string _usernameMapping;
        private string _fullnameMapping;
        private string _emailMapping;
        private string _nicknameMapping;
        
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private SmtpAdaptor smtpAdaptor;

        public Manager(IServices services, IMetaModel model, string defaultRole, SmtpAdaptor smtpAdaptor)
        {
            this._services = services;
            this._model = model;
            this._defaultRole = defaultRole;
            this.smtpAdaptor = smtpAdaptor;
            _logstring = new StringBuilder();
            _deactivatedMembers = new List<User>();
            _newMembers = new List<User>();
        }

        public bool KeepReactivatedUserProjectAccess
        {
            get { return _reactivatedUserKeepsOrigProjectAccess; }
            set { _reactivatedUserKeepsOrigProjectAccess = value; }
        }

        public bool KeepReactivatedUserDefaultRole
        {
            get { return _reactivatedUserKeepsOrigDefaultRole; }
            set { _reactivatedUserKeepsOrigDefaultRole = value; }
        }

        public bool KeepReactivatedUserPassword
        {
            get { return _reactivatedUserKeepsOrigPassword; }
            set { _reactivatedUserKeepsOrigPassword = value; }
        } 

        public string UsernameMapping
        {
            get { return _usernameMapping; }
            set { _usernameMapping = value;}
        }

        public string FullnameMapping
        {
            get { return _fullnameMapping; }
            set { _fullnameMapping = value; }
        }

        public string EmailMapping
        {
            get { return _emailMapping; }
            set { _emailMapping = value; }
        }

        public string NicknameMapping
        {
            get { return _nicknameMapping; }
            set { _nicknameMapping = value; }
        }

        public IList<User> GetVersionOneUsers()
        {
            IAssetType memberType = _model.GetAssetType(V1Constants.MEMBER);
            Query userQuery = new Query(memberType);

            IAttributeDefinition username = _model.GetAttributeDefinition(V1Constants.USERNAME);
            IAttributeDefinition isInactive = memberType.GetAttributeDefinition("IsInactive");
            IAttributeDefinition scopesAttribute = memberType.GetAttributeDefinition("Scopes");
            userQuery.Selection.Add(username);
            userQuery.Selection.Add(isInactive);
            userQuery.Selection.Add(scopesAttribute);
            
            //FilterTerm term = new FilterTerm(isInactive);
            //term.Equal(false);
            //userQuery.Filter = term;

            QueryResult result = _services.Retrieve(userQuery);

            IList<User> v1UserList = new List<User>();
            v1UserList = BuildV1UsersList(result.Assets);
            return v1UserList;
        }

        public IList<User> BuildLdapUsersList(string serverpath, string groupDN, string username, string pwd)
        {
            try
            {
                LDAPReader ldapReader = new LDAPReader();

                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": Retrieving users from LDAP...");
                LogActionResult(_logstring);

                IList<LDAPUser> ldapUserInfo = ldapReader.GetUsersFromLdap(serverpath,groupDN,username,pwd,_usernameMapping,_fullnameMapping,_emailMapping,_nicknameMapping);

                //testing
                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": Count of users retrieved from LDAP: " + ldapUserInfo.Count.ToString()); //testing
                LogActionResult(_logstring);
                //testing

                IList<User> ldapUsers = new List<User>();

                //Get the Ldapuser data into a Provisioning.User collection.
                foreach (LDAPUser ldapUser in ldapUserInfo)
                {
                    User user = new User();
                    user.Username = ldapUser.Username;
                    user.FullName = ldapUser.FullName;
                    user.Email = ldapUser.Email;
                    user.Nickname = ldapUser.Nickname;

                    ldapUsers.Add(user);

                    //testing
                    _logstring.AppendLine(user.Username + " " + user.FullName + " " + user.Email + " " + user.Nickname);
                    //testing
                }

                LogActionResult(_logstring); //testing
                return ldapUsers;
            }
            catch(Exception ex)
            {
                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + ex.Message.ToString());
                return null;
            }
            finally
            {
                LogActionResult(_logstring);
                
            }
        }

        //public IList<User> CompareUsers1(IList<User> ldapUsers, AssetList versionOneUsers)
        //{
        //    //The "action" List that will be returned 
        //    IList<User> v1ActionList = new List<User>();

        //    //Get just the usernames out of the V1 AssetList
        //    IList<string> v1Usernames = BuildV1UsersList(versionOneUsers);

        //    /*
        //    Loop through the LDAP List, looking for a username match in the V1 usernames List.
        //      - If a username in the LDAP List is not in the V1 List, we add it to the action List for creation in V1.
        //      - If a username is in both LDAP and V1, 
        //        # If the V1 user is active then no action is required. We remove the username from the V1 list. 
        //        # If the V1 user is inactive, then add it to the action list to reactivate in V1, subject to 
        //          configured parameters for project access, default role, and password. 
        //    */

        //    AddNewUsersToActionList(ldapUsers, v1Usernames, v1ActionList);

        //    /*
        //    Now, what remain in the V1 usernames List are the users who are not in the LDAP List.
        //    Add them to the action List for deactivation. 
        //    */

        //    AddInactiveUsersToActionList(v1Usernames, v1ActionList);

        //    return v1ActionList;

        //}

        public IList<User> CompareUsers(IList<User> ldapUsers, IList<User> versionOneUsers)
        {
            //The "action" List that will be returned 
            List<User> v1ActionList = new List<User>(); 
            
            ////Get V1 users out of the V1 AssetList and into a User list.
            //IList<User> v1Usernames = BuildV1UsersList(versionOneUsers);

            /*
            Look for username matches between the Ldap and V1 User lists.
              - A. If a username in the LDAP List is not in the V1 List, add it to the action List for creation in V1.
             
              - B. If a username is in both LDAP and V1, 
                    1. If the V1 user is active, no action is required.  
                    2. If the V1 user is inactive, then add it to the action list to reactivate in V1, subject to 
                       configured parameters for project access, default role, and password. 
             
              - C. If a username is in the V1 list but not in the LDAP list
                    1. If the V1 user is inactive, no action is required
                    2. If the V1 user is active, deactivate it
            */

            bool LdapuserFoundInV1;
            bool V1UserFoundInLDAP;

            //Look for a match using Ldap as the master list...
            foreach (User userInLdap in ldapUsers)
            {
                LdapuserFoundInV1 = false;

                foreach (User userInV1 in versionOneUsers)
                {
                    if (userInLdap.Username == userInV1.Username)
                    {
                        LdapuserFoundInV1 = true;
                        if (userInV1.IsInactive)
                        {
                            //This LDAP user is in V1, but is inactive,
                            //so it needs to be reactivated in V1.
                            userInV1.Reactivate = true;
                            v1ActionList.Add(userInV1);
                        }
                    }
                }

                if (LdapuserFoundInV1 == false)
                {
                    //This LDAP user is not in V1, so it needs to be created in V1.
                    userInLdap.Create = true;
                    v1ActionList.Add(userInLdap);
                }
            }

            //Look for a match using V1 as the master list...           
            foreach (User userInV1 in versionOneUsers)
            {
                V1UserFoundInLDAP = false;
                foreach (User userInLdap in ldapUsers)
                {
                    if (userInLdap.Username == userInV1.Username)
                    {
                        V1UserFoundInLDAP = true;
                    }
                }
                
                if (V1UserFoundInLDAP == false)
                {
                    if (userInV1.IsInactive == false)
                    {
                        //This V1 user is not in Ldap, but is active in V1, 
                        //so it needs to be deactivated in V1.
                        userInV1.Deactivate = true;
                        v1ActionList.Add(userInV1);
                    }
                }  
            }

            return v1ActionList;
        }


        public void UpdateVersionOne(IList<User> actionList)
        {
            /*
             * Takes the action list that resulted from the comparison between 
             * the LDAP user list and the V1 user list, and takes the appropriate 
             * action in V1. 
            */
            StringCollection deactivatedUsers = new StringCollection();
            StringCollection addedUsers = new StringCollection();

            foreach (User user in actionList)
            {
                if (user.Deactivate)
                {
                    DeactivateVersionOneMember(user);
                    deactivatedUsers.Add(user.Username);
                }
                else if (user.Create)
                {
                    CreateNewVersionOneMember(user);
                    addedUsers.Add(user.Username);
                }
                else if (user.Reactivate)
                {
                    ReactivateVersionOneMember(user);
                }
                else if (user.Delete)
                {
                    DeleteVersionOneMember(user);
                }
            }

            //Flush the cached messaging to the log file
            LogActionResult(_logstring);
            if (addedUsers.Count > 0 || deactivatedUsers.Count > 0)
            {
                smtpAdaptor.SendAdminNotification(addedUsers, deactivatedUsers);
            }
        }

        private IList<string> BuildV1UsernamesList1(AssetList versionOneUsers)
        {
            IList<string> v1Usernames = new List<string>();
            IAttributeDefinition usernameAttribute = _model.GetAttributeDefinition(V1Constants.USERNAME);

            foreach (Asset userinV1 in versionOneUsers)
            {
                v1Usernames.Add(userinV1.GetAttribute(usernameAttribute).Value.ToString().Trim());
            }
            return v1Usernames;
        }

        private IList<User> BuildV1UsersList(AssetList versionOneUsers)
        {
            IList<User> v1Usernames = new List<User>();
            IAttributeDefinition usernameAttribute = _model.GetAttributeDefinition(V1Constants.USERNAME);
            IAttributeDefinition isInactiveAttribute = _model.GetAttributeDefinition(V1Constants.ISINACTIVE);
            
            foreach (Asset userinV1 in versionOneUsers)
            {
                User v1User = new User();
                v1User.Username = userinV1.GetAttribute(usernameAttribute).Value.ToString().Trim();
                
                string strInactive = userinV1.GetAttribute(isInactiveAttribute).Value.ToString().Trim();
                
                if (strInactive.ToUpper() == "TRUE")
                {
                    v1User.IsInactive = true; 
                }
                else
                {
                    v1User.IsInactive = false;
                }

                v1Usernames.Add(v1User);
            }
            return v1Usernames;
        }

        private void DeactivateVersionOneMember(User user)
        {
            string resultString = "";
            string uname = user.Username;

            try
            {
                Oid memberOid = GetMemberOidByUsername(uname);
                IOperation inactivateMember = _model.GetOperation(V1Constants.INACTIVATE);
                Oid inactivatedOid = _services.ExecuteOperation(inactivateMember, memberOid);

                user.Oid = memberOid;
                _deactivatedMembers.Add(user);
                
                resultString = "Member with username '" + uname + "' has been deactivated in the VersionOne system.";
            }

            catch (Exception)
            {
                resultString = "Attempt to deactivate Member with username '" + uname + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);    
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
                IAssetType memberType = _model.GetAssetType(V1Constants.MEMBER);
                Asset newMember = _services.New(memberType, null);
                IAttributeDefinition usernameAttribute = memberType.GetAttributeDefinition("Username");
                IAttributeDefinition passwordAttribute = memberType.GetAttributeDefinition("Password");
                IAttributeDefinition nameAttribute = memberType.GetAttributeDefinition("Name");
                IAttributeDefinition nicknameAttribute = memberType.GetAttributeDefinition("Nickname");
                IAttributeDefinition emailAttribute = memberType.GetAttributeDefinition("Email");
                IAttributeDefinition defaultRoleAttribute = memberType.GetAttributeDefinition("DefaultRole");
                string password = GeneratePassword(username);
                newMember.SetAttributeValue(usernameAttribute, username);
                newMember.SetAttributeValue(passwordAttribute, password);
                newMember.SetAttributeValue(nameAttribute, user.FullName);
                newMember.SetAttributeValue(nicknameAttribute, user.Nickname);
                newMember.SetAttributeValue(emailAttribute, user.Email);
                newMember.SetAttributeValue(defaultRoleAttribute, this._defaultRole);  

                _services.Save(newMember);
                
                //Verify that the V1 user was created
                Oid newMemberOid = GetMemberOidByUsername(username);
                if (newMemberOid != null)
                {
                    user.Oid = newMemberOid;
                    _newMembers.Add(user);
                    
                    resultString = "Member with username '" + username + "' has been created in the VersionOne system.";
                    smtpAdaptor.SendUserNotification(username, password, user.Email);
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
                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);    
            }
        }

        private string GeneratePassword(string username)
        {
            return username + Guid.NewGuid().ToString().Substring(0, 6);
        }

        private Oid GetMemberOidByUsername(string username)
        {
            IAssetType memberType = _model.GetAssetType(V1Constants.MEMBER);
            Query userQuery = new Query(memberType);
            IAttributeDefinition usernameAttribute = memberType.GetAttributeDefinition("Username");
            userQuery.Selection.Add(usernameAttribute);

            FilterTerm term = new FilterTerm(usernameAttribute);
            term.Equal(username);
            userQuery.Filter = term;

            QueryResult result = _services.Retrieve(userQuery);
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
            /***
             * Reactivation is subject to a few parameters set in configuration.  
             * These determine the user's Project Access, Deault Role, and Password upon reactivation.
             ****/ 
            
            string resultString = "";
            string uname = deactivatedUser.Username;
            
            try
            {
                Oid memberOid = GetMemberOidByUsername(uname);

                if (!_reactivatedUserKeepsOrigProjectAccess)
                {
                    //Remove all project access for this user 
                    RemoveAllProjectAccessFromUser(memberOid);
                }

                //Make any required attribute changes
                if (!_reactivatedUserKeepsOrigDefaultRole || !_reactivatedUserKeepsOrigPassword)
                {
                    Query query = new Query(memberOid);
                    IAssetType memberType = _model.GetAssetType("Member");
                    QueryResult result = _services.Retrieve(query);
                    Asset member = result.Assets[0];
                    
                    if (!_reactivatedUserKeepsOrigDefaultRole)
                    {
                        //assign the user the default role
                        IAttributeDefinition defaultRoleAttribute = memberType.GetAttributeDefinition("DefaultRole");
                        member.SetAttributeValue(defaultRoleAttribute, _defaultRole);
                        query.Selection.Add(defaultRoleAttribute);
                    }

                    if (!_reactivatedUserKeepsOrigPassword)
                    {
                        //assign the user a generated password
                        IAttributeDefinition passwordAttribute = memberType.GetAttributeDefinition("Password");
                        member.SetAttributeValue(passwordAttribute, GeneratePassword(uname));
                        query.Selection.Add(passwordAttribute);
                    }

                 _services.Save(member);

                }

                //Reactivate the user
                IOperation reactivateMember = _model.GetOperation(V1Constants.REACTIVATE);
                Oid inactivatedOid = _services.ExecuteOperation(reactivateMember, memberOid);

                resultString = "Member with username '" + uname + "' has been reactivated in the VersionOne system.";
            }

            catch (Exception)
            {
                resultString = "Attempt to reactivate Member with username '" + uname + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);
            }

        }

        private void RemoveAllProjectAccessFromUser(Oid memberOid)
        {
            Query query = new Query(memberOid);
            IAssetType memberType = _model.GetAssetType("Member");
            IAttributeDefinition scopesAttribute = memberType.GetAttributeDefinition("Scopes");
            query.Selection.Add(scopesAttribute);
            QueryResult result = _services.Retrieve(query);
            Asset member = result.Assets[0];

            IEnumerable associatedProjects = member.GetAttribute(scopesAttribute).Values;

            string strresult = "";

            foreach (var scope in associatedProjects)
            {
                member.RemoveAttributeValue(scopesAttribute, scope.ToString());
            }
        }

        public void DeleteVersionOneMember(User userToDelete)
        {
            string resultString = "";
            string uname = userToDelete.Username;

            try
            {
                Oid memberOid = GetMemberOidByUsername(uname);

                IOperation deleteMember = _model.GetOperation(V1Constants.DELETE);
                Oid deletedOid = _services.ExecuteOperation(deleteMember, memberOid);

                resultString = "Member with username '" + uname + "' has been deleted in the VersionOne system.";
            }

            catch (Exception)
            {
                resultString = "Attempt to delete Member with username '" + uname + "' in the VersionOne system has FAILED.";
            }

            finally
            {
                /* Cache the result for logging */
                _logstring.AppendLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:ffff") + ": " + resultString);
            }

        }

        public void LogActionResult(StringBuilder message)
        {
            logger.Info(message);
        }

        
      }
        
    }
