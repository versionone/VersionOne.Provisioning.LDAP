﻿/*(c) Copyright 2011, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using NLog;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning {
    public class Manager {
        private readonly IServices _services;
        private readonly IMetaModel _model;
        private readonly string _defaultRole;
        private readonly IUserDirectoryReader _directoryReader;
        private readonly ISmtpAdapter _smtpAdapter;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Manager(V1Instance v1, ISmtpAdapter smtpAdapter, IUserDirectoryReader ldapreader) {
            _services = v1.Services;
            _model = v1.Model;
            _defaultRole = v1.DefaultRole;
            _smtpAdapter = smtpAdapter;
            _directoryReader = ldapreader;
        }


        public IDictionary<string, User> GetVersionOneUsers() {
            IAssetType memberType = _model.GetAssetType(V1Constants.MEMBER);
            Query userQuery = new Query(memberType);

            IAttributeDefinition username = _model.GetAttributeDefinition(V1Constants.USERNAME);
            IAttributeDefinition isInactive = _model.GetAttributeDefinition(V1Constants.ISINACTIVE);
            IAttributeDefinition ckInactivate = _model.GetAttributeDefinition(V1Constants.CHECKINACTIVATE);

            userQuery.Selection.Add(username);
            userQuery.Selection.Add(isInactive);
            userQuery.Selection.Add(ckInactivate);
            QueryResult result = _services.Retrieve(userQuery);

            IDictionary<string, User> v1UserList = BuildV1UsersList(result.Assets);

            logger.Info(v1UserList.Count + " users successfully retrieved from VersionOne.");
            //[count] users successfully retrieved from V1...

            return v1UserList;
        }

        public IDictionary<string, User> GetDirectoryUsers() {
            IDictionary<string, User> users = new Dictionary<string, User>();
            try {
                IList<DirectoryUser> directoryUsers = _directoryReader.GetUsers();

                logger.Info(directoryUsers.Count + " directory members retrieved.");
                
                //Get the Ldapuser data into a Provisioning.User collection.
                foreach (DirectoryUser directoryUser in directoryUsers) {
                    string userName = directoryUser.Username.ToLowerInvariant();

                    if (!users.ContainsKey(userName)) {
                        User user = new User();
                        user.Username = directoryUser.Username;
                        user.FullName = directoryUser.FullName;
                        user.Email = directoryUser.Email;
                        user.Nickname = directoryUser.Nickname;
                        users.Add(userName, user);

                        logger.Debug("Member retrieved from directory: " + user.Username);
                    } else {
                        logger.Error("Duplicate username found in directory: " + directoryUser.Username +
                                     "; ignoring user with duplicate username.");
                    }
                }
            } catch (Exception ex) {
                logger.ErrorException("Error retrieving users from Directory", ex);
                throw;
            }
            return users;
        }

        public IList<User> CompareUsers(IDictionary<string, User> directoryUsers,
                                        IDictionary<string, User> versionOneUsers) {
            List<User> v1ActionList = new List<User>();

            List<User> v1UsersToCreate = new List<User>();
            int countUsersToCreate = 0;
            List<User> v1UsersToReactivate = new List<User>();
            int countUsersToReactivate = 0;
            List<User> v1UsersToDeactivate = new List<User>();
            int countUsersToDeactivate = 0;

            //Look for a match using directory as the master list...
            foreach (User directoryUser in directoryUsers.Values) {
                string directoryUserName = directoryUser.Username.ToLowerInvariant();

                if (versionOneUsers.ContainsKey(directoryUserName)) {
                    User userInV1 = versionOneUsers[directoryUserName];
                    if (userInV1.IsInactive) {
                        //This LDAP user is in V1, but is inactive,
                        //so it needs to be reactivated in V1.
                        userInV1.Reactivate = true;
                        userInV1.Email = directoryUser.Email;
                        v1UsersToReactivate.Add(userInV1);
                        countUsersToReactivate++;
                    }
                } else {
                    //This directory user is not in V1, so it needs to be created in V1.
                    directoryUser.Create = true;
                    v1UsersToCreate.Add(directoryUser);
                    countUsersToCreate++;
                }
            }

            //Look for a match using V1 as the master list...           
            foreach (User userInV1 in versionOneUsers.Values) {
                string v1UserName = userInV1.Username.ToLowerInvariant();

                if (!directoryUsers.ContainsKey(v1UserName)) {
                    if (userInV1.CheckInactivate && userInV1.V1MemberAsset.Oid.Token != V1Constants.DEFAULTADMINOID) {
                        //This V1 user is not in Ldap, but is active in V1, 
                        //so it needs to be deactivated in V1.
                        userInV1.Deactivate = true;
                        v1UsersToDeactivate.Add(userInV1);
                        countUsersToDeactivate++;
                    }
                }
            }

            // Put V1 users to deactivate in the list first
            v1ActionList.AddRange(v1UsersToDeactivate);
            // Add the rest of the users
            v1ActionList.AddRange(v1UsersToReactivate);
            v1ActionList.AddRange(v1UsersToCreate);

            logger.Info(countUsersToCreate + " Directory users have been marked for creation in VersionOne.");
            logger.Info(countUsersToDeactivate + " VersionOne users have been marked for deactivation.");
            logger.Info(countUsersToReactivate + " VersionOne users have been marked for reactivation.");

            return v1ActionList;
        }


        public void UpdateVersionOne(IList<User> actionList) {
            /*
             * Takes the action list that resulted from the comparison between 
             * the Directory user list and the V1 user list, and takes the appropriate 
             * action in V1. 
            */
            StringCollection deactivatedUsers = new StringCollection();
            StringCollection addedUsers = new StringCollection();
            StringCollection reactivatedUsers = new StringCollection();

            foreach (User user in actionList) {
                if (user.Deactivate) {
                    DeactivateVersionOneMember(user);
                    deactivatedUsers.Add(user.Username);
                } else if (user.Create) {
                    CreateNewVersionOneMember(user);
                    addedUsers.Add(user.Username);
                } else if (user.Reactivate) {
                    ReactivateVersionOneMember(user);
                    reactivatedUsers.Add(user.Username);
                } else if (user.Delete) {
                    DeleteVersionOneMember(user);
                }
            }

            if (addedUsers.Count > 0 || deactivatedUsers.Count > 0 || reactivatedUsers.Count > 0) {
                _smtpAdapter.SendAdminNotification(addedUsers, reactivatedUsers, deactivatedUsers);
            }
        }


        private IDictionary<string, User> BuildV1UsersList(AssetList versionOneUsers) {
            IDictionary<string, User> v1Usernames = new Dictionary<string, User>();
            IAttributeDefinition usernameAttribute = _model.GetAttributeDefinition(V1Constants.USERNAME);
            IAttributeDefinition isInactiveAttribute = _model.GetAttributeDefinition(V1Constants.ISINACTIVE);
            IAttributeDefinition ckInactivate = _model.GetAttributeDefinition(V1Constants.CHECKINACTIVATE);

            foreach (Asset userinV1 in versionOneUsers) {
                User v1User = new User();
                object usernameAttributeValue = userinV1.GetAttribute(usernameAttribute).Value;

                if (usernameAttributeValue != null) {
                    v1User.Username = usernameAttributeValue.ToString().Trim();
                    v1User.V1MemberAsset = userinV1;
                    v1User.CheckInactivate = bool.Parse(userinV1.GetAttribute(ckInactivate).Value.ToString());
                    v1User.IsInactive = bool.Parse(userinV1.GetAttribute(isInactiveAttribute).Value.ToString());
                    v1Usernames.Add(v1User.Username.ToLowerInvariant(), v1User);
                }
            }
            return v1Usernames;
        }

        private void DeactivateVersionOneMember(User user) {
            string username = user.Username;

            try {
                IOperation inactivateMember = _model.GetOperation(V1Constants.INACTIVATE);
                Oid deactivatedOid = _services.ExecuteOperation(inactivateMember, user.V1MemberAsset.Oid);

                logger.Info("Member with username '" + username + "' has been DEACTIVATED in the VersionOne system.");
            } catch (Exception ex) {
                logger.ErrorException(
                    "Attempt to deactivate Member with username '" + username + "' in the VersionOne system has FAILED.",
                    ex);
            }
        }

        private void CreateNewVersionOneMember(User user) {
            string username = user.Username;
            string password = "";
            if (!UseIntegratedAuthorization()) {
                password = GeneratePassword(username);
            }

            try {
                IAssetType memberType = _model.GetAssetType(V1Constants.MEMBER);
                Asset newMember = _services.New(memberType, null);
                IAttributeDefinition usernameAttribute = memberType.GetAttributeDefinition("Username");
                IAttributeDefinition passwordAttribute = memberType.GetAttributeDefinition("Password");
                IAttributeDefinition nameAttribute = memberType.GetAttributeDefinition("Name");
                IAttributeDefinition nicknameAttribute = memberType.GetAttributeDefinition("Nickname");
                IAttributeDefinition emailAttribute = memberType.GetAttributeDefinition("Email");
                IAttributeDefinition defaultRoleAttribute = memberType.GetAttributeDefinition("DefaultRole");
                newMember.SetAttributeValue(usernameAttribute, username);
                newMember.SetAttributeValue(passwordAttribute, password);
                newMember.SetAttributeValue(nameAttribute, user.FullName);
                newMember.SetAttributeValue(nicknameAttribute, user.Nickname);
                newMember.SetAttributeValue(emailAttribute, user.Email);
                newMember.SetAttributeValue(defaultRoleAttribute, this._defaultRole);
                _services.Save(newMember);
                user.V1MemberAsset = newMember;


                logger.Info("Member with username '" + username + "' has been CREATED in the VersionOne system.");
                NotifyUser(user, username, password);
            } catch (Exception ex) {
                logger.ErrorException(
                    "Attempt to create Member with username '" + username + "' in the VersionOne system has FAILED.", ex);
            }
        }

        private void NotifyUser(User user, string username, string password) {
            try {
                _smtpAdapter.SendUserNotification(username, password, user.Email);

            } catch (Exception error) {
                logger.Error("Failed to send notification to user: " + username + ": " + error.Message);
            }
        }

        private static string GeneratePassword(string username) {
            return username + Guid.NewGuid().ToString().Substring(0, 6);

        }


        public void ReactivateVersionOneMember(User userToReactivate) {
            /***
             * Reactivation is subject to a few parameters set in configuration.  
             * These determine the user's Project Access, Default Role, and Password upon reactivation.
             ****/

            string username = userToReactivate.Username;
            Oid memberOid = userToReactivate.V1MemberAsset.Oid;

            try {
                Asset member = userToReactivate.V1MemberAsset;

                //Reactivate the user
                IOperation reactivateMember = _model.GetOperation(V1Constants.REACTIVATE);
                _services.ExecuteOperation(reactivateMember, memberOid);

                //Assign the user a new password
                IAttributeDefinition passwordAttribute = _model.GetAttributeDefinition(V1Constants.PASSWORD);
                string newPassword = GeneratePassword(username);
                member.SetAttributeValue(passwordAttribute, newPassword);
                _services.Save(member);


                _smtpAdapter.SendUserNotification(username, newPassword, userToReactivate.Email);
                logger.Info("Member with username '" + username + "' has been REACTIVATED in the VersionOne system.");
            } catch (Exception ex) {
                logger.ErrorException(
                    "Attempt to reactivate Member with username '" + username + "' in the VersionOne system has FAILED." +
                    ex, ex);
            }
        }

        public void DeleteVersionOneMember(User userToDelete) {
            string uname = userToDelete.Username;

            try {
                Oid memberOid = userToDelete.V1MemberAsset.Oid;

                IOperation deleteMember = _model.GetOperation(V1Constants.DELETE);
                _services.ExecuteOperation(deleteMember, memberOid);

                logger.Info("Member with username '" + uname + "' has been DELETED in the VersionOne system.");
            } catch (Exception ex) {
                logger.ErrorException(
                    "Attempt to delete Member with username '" + uname + "' in the VersionOne system has FAILED.", ex);
            }
        }

        private bool UseIntegratedAuthorization() {
            return ConfigurationManager.AppSettings["IntegratedAuth"].Equals("true");
        }

    }
}