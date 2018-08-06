using System;
using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;

namespace VersionOne.Provisioning.Tests {
    [TestFixture]
    [Ignore("Integrational tests would not run on build server.")]
    public class ManagerTests {
        private Manager manager;
        private TestUserFactory testUserFactory;

        [SetUp]
        public void SetUp() {
            V1Instance v1 = Factory.GetV1Instance();
            testUserFactory = new TestUserFactory(v1);
            IUserDirectoryReader ldapReader = new DirectoryReaderStub();
            ISmtpAdapter smtpAdapter = new SmtpAdapterStub();
            manager = new Manager(v1, smtpAdapter, ldapReader);
        }

        [Test]
        public void TestBuildLDAPUsersList() {
            IDictionary<string, User> ldapUsersList = manager.GetDirectoryUsers();
            Assert.AreEqual(2, ldapUsersList.Count);
        }

        [Test]
        public void TestGetVersionOneUsers() {
            //call an instance of manager
            IDictionary<string, User> versionOneUsers = manager.GetVersionOneUsers();
            Assert.Greater(versionOneUsers.Count, 0);
        }

        [Test]
        public void TestCompareUsers() {
            IDictionary<string, User> usersFromLdap = testUserFactory.CreateTestLdapUsers(false);
            IDictionary<string, User> usersFromV1 = testUserFactory.CreateTestV1Users(false);
            Assert.AreEqual(5, usersFromLdap.Count); //make sure the usersFromLdap List was populated correctly

            Assert.AreEqual(4, usersFromV1.Count);  //make sure the V1 users List was populated correctly

            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);

            Assert.AreEqual(5, usersToAction.Count);

            foreach (User user in usersToAction) {
                string userName = user.Username;

                CheckUser(user, userName);
            }
        }

        [Test]
        public void TestCompareUsersIgnoreCaseLdapUpper() {
            IDictionary<string, User> usersFromLdap = testUserFactory.CreateTestLdapUsers(true);
            Assert.AreEqual(5, usersFromLdap.Count);

            IDictionary<string, User> usersFromV1 = testUserFactory.CreateTestV1Users(false);
            Assert.AreEqual(4, usersFromV1.Count);

            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);
            Assert.AreEqual(5, usersToAction.Count);
            
            foreach (User user in usersToAction) {
                string userName = user.Username.ToLowerInvariant();

                CheckUser(user, userName);
            }
        }

        [Test]
        public void TestCompareUsersIgnoreCaseV1Upper() {
            IDictionary<string, User> usersFromLdap = testUserFactory.CreateTestLdapUsers(false);
            Assert.AreEqual(5, usersFromLdap.Count);

            IDictionary<string, User> usersFromV1 = testUserFactory.CreateTestV1Users(true);
            Assert.AreEqual(4, usersFromV1.Count);

            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);
            Assert.AreEqual(5, usersToAction.Count);
            
            foreach (User user in usersToAction) {
                string userName = user.Username.ToLowerInvariant();

                CheckUser(user, userName);
            }
        }

        [Test]
        public void TestCompareUsersOrder() {
            IDictionary<string, User> usersFromLdap = testUserFactory.CreateTestLdapUsers(false);
            Assert.AreEqual(5, usersFromLdap.Count); //make sure the usersFromLdap List was populated correctly
        
            IDictionary<string, User> usersFromV1 = testUserFactory.CreateTestV1Users(false);
            Assert.AreEqual(4, usersFromV1.Count); //make sure the V1 users List was populated correctly
        
            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);
        
            Assert.AreEqual(5, usersToAction.Count);
        
            CheckUser(usersToAction[0], "tom"); // make sure that Deactivation of users is first in the list
        }

        private static void CheckUser(User user, string userName) {
            switch (userName) {
                case "abe":
                    Assert.IsTrue(user.Create);
                    Assert.IsFalse(user.Reactivate);
                    Assert.IsFalse(user.Deactivate);
                    break;
                case "ben":
                    Assert.IsTrue(user.Create);
                    Assert.IsFalse(user.Reactivate);
                    Assert.IsFalse(user.Deactivate);
                    break;
                case "cam":
                    Assert.IsTrue(user.Create);
                    Assert.IsFalse(user.Reactivate);
                    Assert.IsFalse(user.Deactivate);
                    break;
                case "val":
                    Assert.IsFalse(user.Create);
                    Assert.IsTrue(user.Reactivate);
                    Assert.IsFalse(user.Deactivate);
                    break;
                case "tom":
                    Assert.IsFalse(user.Create);
                    Assert.IsFalse(user.Reactivate);
                    Assert.IsTrue(user.Deactivate);
                    break;
                default:
                    Assert.Fail("Unexpected user in the list");
                    break;
            }
        }

        [Test]
        public void TestUpdateVersionOne() {
            IDictionary<string, User> v1Users = manager.GetVersionOneUsers();
            int currentCount = v1Users.Count;

            IList<User> usersToAction = new List<User>();

            User userToAdd = testUserFactory.CreateUserToAdd("Fred");
            usersToAction.Add(userToAdd);

            User userToDeactivate = testUserFactory.CreateUserToDeactivate("andre");
            usersToAction.Add(userToDeactivate);

            User userToDeactivate2 = testUserFactory.CreateUserToDeactivate("joe");
            usersToAction.Add(userToDeactivate2);

            Assert.AreEqual(3, usersToAction.Count);

            manager.UpdateVersionOne(usersToAction);

            v1Users = manager.GetVersionOneUsers();
            Assert.AreEqual(currentCount + 1, v1Users.Count);

            ResetV1Data(userToAdd, userToDeactivate, userToDeactivate2);
        }

        [Test]
        public void TestConfigurationValidationGood() {
            try {
                Factory.ValidateConfiguration();
            } catch (Exception error) {
                Assert.Fail(error.Message);
            }
        }
        [Test]
        public void TestMissingVersionOneConfigValues() {
            bool caughtException = false;
            try {
                InvalidateAppSettings();
                Factory.ValidateConfiguration();
            } catch (Exception) {
                caughtException = true;
            }
            Assert.IsTrue(caughtException, "Didn't catch a missing value");
        }

        [Test]
        public void TestBadConnectionSettings() {
            string instanceLocation = ConfigurationManager.AppSettings["V1Instance"];
            string userName = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string userPassword = ConfigurationManager.AppSettings["V1InstancePassword"];
            ConfigurationManager.AppSettings.Set("V1Instance", "http://bobsyouruncle");
            bool connectionWorked = Factory.CheckConnectionValid();
            Assert.IsFalse(connectionWorked, "should have failed to connect to bad address");
            ConfigurationManager.AppSettings.Set("V1Instance", instanceLocation);
            ConfigurationManager.AppSettings.Set("V1InstanceUsername", "notarealuser");
            connectionWorked = Factory.CheckConnectionValid();
            Assert.IsFalse(connectionWorked, "Bad username should have failed");
            ConfigurationManager.AppSettings.Set("V1InstanceUsername", userName);
            ConfigurationManager.AppSettings.Set("V1InstancePassword", "badpassword");
            connectionWorked = Factory.CheckConnectionValid();
            Assert.IsFalse(connectionWorked, "Bad password should have failed");
            ConfigurationManager.AppSettings.Set("v1InstancePassword", userPassword);
        }

        [Test]
        public void TestUseIntegratedAuth() {
            ConfigurationManager.AppSettings.Set("IntegratedAuth", "true");
            ConfigurationManager.AppSettings.Set("V1InstanceUsername", "");
            ConfigurationManager.AppSettings.Set("V1InstancePassword", "");

            string instanceLocation = ConfigurationManager.AppSettings.Get("V1InstanceIntegratedAuth");
            ConfigurationManager.AppSettings.Set("V1Instance", instanceLocation);

            V1Instance v1Authorized = Factory.GetV1Instance();
            IUserDirectoryReader ldapReader = new DirectoryReaderStub();
            ISmtpAdapter smtpAdapter = new SmtpAdapterStub();
            Manager authorizedManager = new Manager(v1Authorized, smtpAdapter, ldapReader);
            Assert.IsNotNull(authorizedManager);
            IDictionary<string, User> users = authorizedManager.GetVersionOneUsers();
            Assert.Greater(users.Count, 0);
        }

        private void InvalidateAppSettings() {
            ConfigurationManager.AppSettings.Set("V1Instance", "");
            ConfigurationManager.AppSettings.Set("V1InstanceUserName", "");
            ConfigurationManager.AppSettings.Set("V1InstancePassword", "");
            ConfigurationManager.AppSettings.Set("V1UserDefaultRole", "");
            ConfigurationManager.AppSettings.Set("IntegratedAuth", "");
            ConfigurationManager.AppSettings.Set("ldapGroupMemberAttribute", "");
        }

        private void ResetV1Data(User userToAdd, User userToDeactivate, User userToDeactivate2) {
            IList<User> usersToUndo = new List<User>();
            userToAdd.Create = false;
            userToAdd.Delete = true;
            userToDeactivate.Deactivate = false;
            userToDeactivate.Reactivate = true;
            userToDeactivate2.Deactivate = false;
            userToDeactivate2.Reactivate = true;
            usersToUndo.Add(userToAdd);
            usersToUndo.Add(userToDeactivate);
            usersToUndo.Add(userToDeactivate2);
            manager.UpdateVersionOne(usersToUndo);
        }
    }
}
