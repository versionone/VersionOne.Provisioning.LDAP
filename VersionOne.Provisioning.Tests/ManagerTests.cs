using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable RedundantUsingDirective
using System.Text;
// ReSharper restore RedundantUsingDirective
using NUnit.Framework;
using VersionOne.SDK.APIClient;

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

        [SetUp]
        public void SetUp()
        {
            IAPIConnector metaConnector = new V1APIConnector("http://localhost/demo/meta.v1/");
            IAPIConnector servicesConnector = new V1APIConnector("http://localhost/demo/rest-1.v1/", "admin", "admin");
            
            model = new MetaModel(metaConnector);
            services = new Services(model,servicesConnector);
            manager = new Manager(services, model,"Role:4");
            usernameAttribute = model.GetAttributeDefinition(V1Constants.USERNAME);
            GetTestV1Users();
        }

        [TearDown]
        public void TearDown()
        {
            //Delete any users created by the unit tests
            if (manager.newMembers.Count > 0)
            {
                foreach (User userAdded in manager.newMembers)
                {
                    manager.DeleteVersionOneMember(userAdded);
                }

            }

            //Reactivate any users deactivated by the unit tests
            if (manager.deactivatedMembers.Count > 0)
            {
                foreach (User inactiveMember in manager.deactivatedMembers)
                {
                    manager.ReactivateVersionOneMember(inactiveMember);
                }
            }

            //Flush any cached messaging to the log file
            manager.LogActionResult(manager.logstring);
        }

        [Test]
        public void TestGetVersionOneUsers()
        {
            //call an instance of manager
            AssetList versionOneUsers = manager.GetVersionOneUsers();
            Assert.AreEqual(11,versionOneUsers.Count);
            bool weFoundAndre = false;

            foreach (Asset asset in versionOneUsers)
            {
                string userName = asset.GetAttribute(usernameAttribute).Value.ToString();
                if(userName == "andre")
                {
                    weFoundAndre = true;
                }
            }
            Assert.IsTrue(weFoundAndre);
        }

        //[Test]
        public void TestCompareUsers()
        {
            IList<User> usersFromLdap = CreateTestLdapUsers();
            Assert.AreEqual(10, usersFromLdap.Count); //make sure the usersFromLdap List was populated correctly

            Assert.AreEqual(11, usersFromV1.Count);  //make sure the V1 users List was populated correctly

            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);
            //userstoAction should contain the user objects from the array passed in that need to be created
            //as well as some new user objects that simply have username and deactivate=true

            //based on what you put in the usersFromLdap array, test for how many users should be in the userstoUpdate List
            //and check that the correct ones are set to Create or Deactivate.
            Assert.AreEqual(2, usersToAction.Count);
            Assert.IsTrue(usersToAction[0].Username == "Fred");
            Assert.IsTrue(usersToAction[1].Username == "vijay");
            Assert.IsTrue(usersToAction[0].Create == true);
            Assert.IsTrue(usersToAction[1].Deactivate == true);

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

            AssetList v1Users = manager.GetVersionOneUsers();
            Assert.AreEqual(12, v1Users.Count);

        }

        private IList<User> CreateTestLdapUsers()
        {
            IList<User> users = new List<User>();
            
            //Put some users in here that can be compared to demo data.
            //First, users that will not need to be changed. Leaving out "vijay" (so he should eventually be deactivated).

            string[] testUsers = {"alfred", "andre", "boris", "claus", "danny", "joe", "sara", "tammy", "willy"};

            for (int i = 0; i < testUsers.Length; i++)
            {
                {
                    string username = testUsers[i];
                    User user = CreateTestUser(username);
                    users.Add(user);
                }
            }

           //Now, an LDAP user that does not appear in the demo data...
            User user10 = CreateTestUser("Fred");
            users.Add(user10);

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
            user.UserToDeactivate = GetTestV1User(username);
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

        private void GetTestV1Users()
        {
            usersFromV1 = manager.GetVersionOneUsers();
        }
       }
}
