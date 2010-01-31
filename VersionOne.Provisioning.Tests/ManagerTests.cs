using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable RedundantUsingDirective
using System.Text;
// ReSharper restore RedundantUsingDirective
using NUnit.Framework;
using VersionOne.SDK.APIClient;
using System.Configuration;

namespace VersionOne.Provisioning.Tests
{
    [TestFixture]
    public class ManagerTests
    {
        private Manager manager;
        private IMetaModel model;
        private IServices services;
        private AssetList usersFromV1;
        private IAttributeDefinition usernameAttribute;
        private IAttributeDefinition isInactiveAttribute;
        private string _V1Instance;
        private string _V1Login;
        private string _V1Password;
        private string _V1DefaultRole;
        private string _ldapServerPath;
        private string _ldapGroupDN;
        private string _ldapUsername;
        private string _ldapPassword;
        private string _logpath;
        private bool _preserveReactivatedUserProjectAccess;
        private bool _preserveReactivatedUserDefaultRole;
        private bool _preserveReactivatedUserPassword;
        private string _usernameMapping;
        private string _fullnameMapping;
        private string _emailMapping;
        private string _nicknameMapping;
        private bool _useDefaultLDAPCredentials;

        [SetUp]
        public void SetUp()
        {
            _V1Instance = ConfigurationManager.AppSettings["V1Instance"];
            _V1Login = ConfigurationManager.AppSettings["V1InstanceUsername"];
            _V1Password = ConfigurationManager.AppSettings["V1InstancePassword"];
            _V1DefaultRole = ConfigurationManager.AppSettings["V1UserDefaultRole"];
            _ldapServerPath = ConfigurationManager.AppSettings["ldapServerPath"];
            _ldapGroupDN = ConfigurationManager.AppSettings["ldapGroupDN"];
            _ldapUsername = ConfigurationManager.AppSettings["ldapUsername"];
            _ldapPassword = ConfigurationManager.AppSettings["ldapPassword"];
            _logpath = ConfigurationManager.AppSettings["logPath"];
            _usernameMapping = ConfigurationManager.AppSettings["mapToV1Username"];
            _fullnameMapping = ConfigurationManager.AppSettings["mapToV1Fullname"]; 
            _emailMapping = ConfigurationManager.AppSettings["mapToV1Email"]; 
            _nicknameMapping = ConfigurationManager.AppSettings["mapToV1Nickname"];

            if(ConfigurationManager.AppSettings["useDefaultLDAPCredentials"].Trim().ToUpper() != "FALSE")
            {
                _useDefaultLDAPCredentials = true;
            }

            IAPIConnector metaConnector = new V1APIConnector(_V1Instance + @"meta.v1/");
            IAPIConnector servicesConnector = new V1APIConnector(_V1Instance + @"rest-1.v1/", _V1Login, _V1Password);
            
            model = new MetaModel(metaConnector);
            services = new Services(model,servicesConnector);
            
            //manager = new Manager(services, model, "Role:4", @"C:\testlogs\samplelog.txt");
            manager = new Manager(services, model, _V1DefaultRole, new SmtpAdaptor(new UserNotificationEmail(), new AdminNotificationEmail()));

            manager.UsernameMapping = _usernameMapping;
            manager.FullnameMapping = _fullnameMapping;
            manager.EmailMapping = _emailMapping;
            manager.NicknameMapping = _nicknameMapping;
            manager.UseDefaultLDAPCredentials = _useDefaultLDAPCredentials;

            usernameAttribute = model.GetAttributeDefinition(V1Constants.USERNAME);
            isInactiveAttribute = model.GetAttributeDefinition(V1Constants.ISINACTIVE);

            //IAssetType memberType = model.GetAssetType(V1Constants.MEMBER);
            //isInactiveAttribute = memberType.GetAttributeDefinition("IsInactive");


            //GetTestV1Users();
        }

        [TearDown]
        public void TearDown()
        {
            //Delete any users created by the unit tests
            if (manager._newMembers.Count > 0)
            {
                foreach (User userAdded in manager._newMembers)
                {
                    manager.DeleteVersionOneMember(userAdded);
                }

            }

            //Reactivate any users deactivated by the unit tests
            if (manager._deactivatedMembers.Count > 0)
            {
                foreach (User inactiveMember in manager._deactivatedMembers)
                {
                    manager.ReactivateVersionOneMember(inactiveMember);
                }
            }
        }

        [Test]
        public void TestBuildLDAPUsersList()
        {
            IList<User> ldapUsersList = new List<User>();
            ldapUsersList = manager.BuildLdapUsersList(_ldapServerPath, _ldapGroupDN, _ldapUsername, _ldapPassword);
            Assert.AreEqual(2, ldapUsersList.Count);

            foreach (User list in ldapUsersList)
            {
                Console.WriteLine(list.Username);
            }
            
        }

        [Test]
        public void TestGetVersionOneUsers()
        {
            //call an instance of manager
            IList<User> versionOneUsers = manager.GetVersionOneUsers();
            Assert.AreEqual(4,versionOneUsers.Count);
            //bool weFoundAndre = false;
            
            Console.WriteLine("----");
            foreach (User v1user in versionOneUsers)
            {
                //string userName = asset.GetAttribute(usernameAttribute).Value.ToString();
                //string inactiveUser = asset.GetAttribute(isInactiveAttribute).Value.ToString().ToUpper();
                
                Console.WriteLine(v1user.Username + " " + v1user.V1MemberAsset.Oid);
                if (v1user.Username == "de")
                {
                    //weFoundAndre = true;
                    Assert.IsTrue(v1user.IsInactive);
                }
            }
            Console.WriteLine("----");
            //Assert.IsTrue(weFoundAndre);
        }

        [Test]
        public void TestCompareUsers()
        {
            IList<User> usersFromLdap = CreateTestLdapUsers();
            IList<User> usersFromV1 = CreateTestV1Users();
            Assert.AreEqual(5, usersFromLdap.Count); //make sure the usersFromLdap List was populated correctly

            Assert.AreEqual(4, usersFromV1.Count);  //make sure the V1 users List was populated correctly

            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);
            //userstoAction should contain the user objects from the array passed in that need to be created
            //as well as some new user objects that simply have username and deactivate=true

            //based on what you put in the usersFromLdap array, test for how many users should be in the userstoUpdate List
            //and check that the correct ones are set to Create or Deactivate.
            Assert.AreEqual(5, usersToAction.Count);

            foreach (User user in usersToAction)
            {
               if (user.Username == "abe")
               {
                   Assert.IsTrue(user.Create == true);
               }
               else if (user.Username == "ben")
               {
                   Assert.IsTrue(user.Create == true);
               }
               else if (user.Username == "cam")
               {
                   Assert.IsTrue(user.Create == true);
               }
               else if (user.Username == "val")
               {
                   Assert.IsTrue(user.Reactivate == true);
               }
               else if (user.Username == "tom")
               {
                   Assert.IsTrue(user.Deactivate == true);
               }
            }
            //Assert.IsTrue(usersToAction[0].Username == "Fred");
            //Assert.IsTrue(usersToAction[1].Username == "vijay");
            //Assert.IsTrue(usersToAction[0].Create == true);
            //Assert.IsTrue(usersToAction[1].Deactivate == true);

        }

        private IList<User> CreateTestV1Users()
        {
            IList<User> testV1Users = new List<User>();

            User user1 = new User();
            user1.Username = "sam";
            user1.IsInactive = false;
            testV1Users.Add(user1);

            User user2 = new User();
            user2.Username = "tom";
            user2.IsInactive = false;
            testV1Users.Add(user2);

            User user3 = new User();
            user3.Username = "val";
            user3.IsInactive = true;
            testV1Users.Add(user3);

            User user4 = new User();
            user4.Username = "jim";
            user4.IsInactive = true;
            testV1Users.Add(user4);

            return testV1Users;
        }

        [Test]
        public void TestUpdateVersionOne()
        {
            IList<User> usersToAction = new List<User>();

            User userToAdd = CreateUserToAdd("Fred");
            usersToAction.Add(userToAdd);

            User userToDeactivate = CreateUserToDeactivate("vijay");
            usersToAction.Add(userToDeactivate);

            User userToDeactivate2 = CreateUserToDeactivate("joe");
            usersToAction.Add(userToDeactivate2);

            Assert.AreEqual(3,usersToAction.Count);

            manager.UpdateVersionOne(usersToAction);

            IList<User> v1Users = manager.GetVersionOneUsers();
            Assert.AreEqual(12, v1Users.Count);

        }

        private IList<User> CreateTestLdapUsers()
        {
            IList<User> users = new List<User>();
            
            //Put some users in here that can be compared to demo data.
            //First, users that will not need to be changed. Leaving out "vijay" (so he should eventually be deactivated).

            string[] testUsers = {"abe", "ben", "cam", "sam", "val"};

            for (int i = 0; i < testUsers.Length; i++)
            {
                {
                    string username = testUsers[i];
                    User user = CreateTestUser(username);
                    users.Add(user);
                }
            }

           //Now, an LDAP user that does not appear in the demo data...
            //User user10 = CreateTestUser("Fred");
            //users.Add(user10);

            return users;
        }

        private User CreateTestUser(string username)
        {
            User user = new User();
            user.Username = username;
            user.FullName = username + " " + username + "son";
            user.Nickname = username;
            user.Email = username + "@test.com";
            return user;
        }

        private User CreateUserToDeactivate(string username)
        {
            User user = CreateTestUser(username);
            user.Deactivate = true;
            user.V1MemberAsset = GetTestV1User(username);
            return user;
        }

        private User CreateUserToAdd (string username)
        {
            User user = CreateTestUser(username);
            user.Create = true;
            return user;

        }

        private Asset GetTestV1User(string username)
        {
            foreach(Asset user in this.usersFromV1)
            {
                if(user.GetAttribute(usernameAttribute).Value.ToString() == username)
                {
                    return user;
                }
            }
            return null;
        }

        //private void GetTestV1Users()
        //{
        //    usersFromV1 = manager.GetVersionOneUsers();
        //}
       }
}
