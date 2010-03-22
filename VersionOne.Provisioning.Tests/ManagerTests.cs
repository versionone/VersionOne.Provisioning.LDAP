using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using NUnit.Framework;
using VersionOne.Provisioning;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning.Tests
{
    [TestFixture]
    public class ManagerTests
    {
        private Manager manager;
        private TestUserFactory testUserFactory;

        [SetUp]
        public void SetUp()
        {
            V1Instance v1 = Factory.GetV1Instance();
            testUserFactory = new TestUserFactory(v1);
            IUserDirectoryReader ldapReader = new DirectoryReaderStub();
            ISmtpAdaptor smtpAdaptor = new SmtpAdaptorStub();
            manager = new Manager(v1, smtpAdaptor, ldapReader);
        }

        [Test]
        public void TestUseIntegratedAuth()
        {

            ConfigurationManager.AppSettings.Set("IntegratedAuth", "true");
            ConfigurationManager.AppSettings.Set("V1InstanceUsername","");
            ConfigurationManager.AppSettings.Set("V1InstancePassword","");
            string instanceLocation = ConfigurationManager.AppSettings.Get("V1InstanceIntegratedAuth");
            ConfigurationManager.AppSettings.Set("V1Instance", instanceLocation);
            V1Instance v1Authorized = Factory.GetV1Instance();
            IUserDirectoryReader ldapReader = new DirectoryReaderStub();
            ISmtpAdaptor smtpAdaptor = new SmtpAdaptorStub();
            Manager authorizedManager = new Manager(v1Authorized, smtpAdaptor, ldapReader);
            Assert.IsNotNull(authorizedManager);
            IDictionary<string, User> users = authorizedManager.GetVersionOneUsers();
            Assert.Greater(users.Count, 0);
        }

        [Test]
        public void TestBuildLDAPUsersList()
        {
            IDictionary<string, User> ldapUsersList = manager.GetDirectoryUsers();
            Assert.AreEqual(2, ldapUsersList.Count);

        }

        [Test]
        public void TestGetVersionOneUsers()
        {
            //call an instance of manager
            IDictionary<string, User> versionOneUsers = manager.GetVersionOneUsers();
            Assert.Greater(versionOneUsers.Count, 0);
        }

        [Test]
        public void TestCompareUsers()
        {
            IDictionary<string, User> usersFromLdap = testUserFactory.CreateTestLdapUsers();
            IDictionary<string, User> usersFromV1 = testUserFactory.CreateTestV1Users();
            Assert.AreEqual(5, usersFromLdap.Count); //make sure the usersFromLdap List was populated correctly

            Assert.AreEqual(4, usersFromV1.Count);  //make sure the V1 users List was populated correctly

            IList<User> usersToAction = manager.CompareUsers(usersFromLdap, usersFromV1);

            Assert.AreEqual(5, usersToAction.Count);

            foreach (User user in usersToAction)
            {
                if (user.Username == "abe")
                {
                    Assert.IsTrue(user.Create);
                }
                else if (user.Username == "ben")
                {
                    Assert.IsTrue(user.Create);
                }
                else if (user.Username == "cam")
                {
                    Assert.IsTrue(user.Create);
                }
                else if (user.Username == "val")
                {
                    Assert.IsTrue(user.Reactivate);
                }
                else if (user.Username == "tom")
                {
                    Assert.IsTrue(user.Deactivate);
                }
            }

        }

        [Test]
        public void TestUpdateVersionOne()
        {
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
        public void TestConfigurationValidationGood()
        {
            try
            {
                Factory.ValidateConfiguration();

            }
            catch (Exception error)
            {

                Assert.Fail(error.Message);
            }

        }
        [Test]
        public void TestMissingVersionOneConfigValues()
        {
            bool caughtException = false;
           try
            {
               InvalidateAppSettings();
                Factory.ValidateConfiguration();
            }
            catch (Exception error)
            {
                caughtException = true;
            }
            Assert.IsTrue(caughtException,"Didn't catch a missing value");
        }

        [Test]
        public void TestBadConnectionSettings()
        {
           
            string instanceLocation = ConfigurationManager.AppSettings["V1Instance"];
            string userName = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string userPassword = ConfigurationManager.AppSettings["V1InstancePassword"];
            ConfigurationManager.AppSettings.Set("V1Instance","http://bobsyouruncle");
            bool connectionWorked = connectionWorked = Factory.CheckConnectionValid();
            Assert.IsFalse(connectionWorked,"should have failed to connect to bad address");
            ConfigurationManager.AppSettings.Set("V1Instance",instanceLocation);
            ConfigurationManager.AppSettings.Set("V1InstanceUsername", "notarealuser");
            connectionWorked = Factory.CheckConnectionValid();
            Assert.IsFalse(connectionWorked,"Bad username should have failed");
            ConfigurationManager.AppSettings.Set("V1InstanceUsername",userName);
            ConfigurationManager.AppSettings.Set("V1InstancePassword","badpassword");
            connectionWorked = Factory.CheckConnectionValid();
            Assert.IsFalse(connectionWorked,"Bad password should have failed");
            ConfigurationManager.AppSettings.Set("v1InstancePassword",userPassword);
        }
       
        private void InvalidateAppSettings()
        {
            ConfigurationManager.AppSettings.Set("V1Instance", "");
            ConfigurationManager.AppSettings.Set("V1InstanceUserName", "");
            ConfigurationManager.AppSettings.Set("V1InstancePassword", "");
            ConfigurationManager.AppSettings.Set("V1UserDefaultRole", "");
            ConfigurationManager.AppSettings.Set("IntegratedAuth","");
            ConfigurationManager.AppSettings.Set("ldapGroupMemberAttribute", "");
        }

        private void ResetV1Data(User userToAdd, User userToDeactivate, User userToDeactivate2)
        {
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

public class TestUserFactory
{
    private IMetaModel model;
    private IServices services;

    public TestUserFactory(V1Instance v1)
    {
        services = v1.Services;
        model = v1.Model;
    }

    public IDictionary<string, User> CreateTestV1Users()
    {
        IDictionary<string, User> testV1Users = new Dictionary<string, User>();
        testV1Users.Add("jim", CreateTestV1User("jim", true));
        testV1Users.Add("sam", CreateTestV1User("sam", false));
        testV1Users.Add("tom", CreateTestV1User("tom", false));
        testV1Users.Add("val", CreateTestV1User("val", true));

        return testV1Users;
    }

    public User CreateTestV1User(string username, bool isinactive)
    {
        User user = new User();
        user.Username = username;
        user.IsInactive = isinactive;
        user.CheckInactivate = !isinactive;
        user.V1MemberAsset = new Asset(new Oid(new TestAssetType(), 1234, 1));
        return user;
    }

    public IDictionary<string, User> CreateTestLdapUsers()
    {
        IDictionary<string, User> users = new Dictionary<string, User>();

        string[] testUsers = { "abe", "ben", "cam", "sam", "val" };

        for (int i = 0; i < testUsers.Length; i++)
        {
            {
                string username = testUsers[i];
                User user = CreateTestUser(username);
                users.Add(user.Username, user);
            }
        }
        return users;
    }


    public User CreateTestUser(string username)
    {
        User user = new User();
        user.Username = username;
        user.FullName = username + " " + username + "son";
        user.Nickname = username;
        user.Email = username + "@test.com";
        return user;
    }

    public User CreateUserToDeactivate(string username)
    {
        User user = CreateTestUser(username);
        user.Deactivate = true;
        IAssetType member = model.GetAssetType("Member");
        Query query = new Query(member);
        FilterTerm term = new FilterTerm(member.GetAttributeDefinition("Username"));
        term.Equal(username);
        query.Filter = term;
        QueryResult result = services.Retrieve(query);
        user.V1MemberAsset = result.Assets[0];
        return user;
    }

    public User CreateUserToAdd(string username)
    {
        User user = CreateTestUser(username);
        user.Create = true;
        return user;

    }
}

public class DirectoryReaderStub : IUserDirectoryReader
{

    public IList<DirectoryUser> GetUsers()
    {
        IList<DirectoryUser> users = new List<DirectoryUser>();
        users.Add(CreateTestUser("superman"));
        users.Add(CreateTestUser("spiderman"));
        return users;
    }

    public void Initialize(NameValueCollection appSettings)
    {
        throw new System.NotImplementedException();
    }

    private DirectoryUser CreateTestUser(string username)
    {
        DirectoryUser user = new DirectoryUser();
        user.Username = username;
        user.FullName = username + " " + username + "son";
        user.Nickname = username;
        user.Email = username + "@test.com";
        return user;
    }
}

public class SmtpAdaptorStub : ISmtpAdaptor
{
    public int UserNotificationCount = 0;
    public int AdminNotificationCount = 0;
    public void SendUserNotification(string username, string password, string to)
    {
        UserNotificationCount++;
    }

    public void SendAdminNotification(StringCollection addedUsernames, StringCollection reactivatedUsernames, StringCollection deactivatedUsernames)
    {
        AdminNotificationCount++;
    }
}

public class TestAssetType : IAssetType
{
    public bool Is(IAssetType targettype)
    {
        throw new System.NotImplementedException();
    }

    public IAttributeDefinition GetAttributeDefinition(string name)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetAttributeDefinition(string name, out IAttributeDefinition def)
    {
        throw new System.NotImplementedException();
    }

    public IOperation GetOperation(string name)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetOperation(string name, out IOperation op)
    {
        throw new System.NotImplementedException();
    }

    public string Token
    {
        get { return "Member"; }
    }

    public IAssetType Base
    {
        get { throw new System.NotImplementedException(); }
    }

    public string DisplayName
    {
        get { throw new System.NotImplementedException(); }
    }

    public IAttributeDefinition DefaultOrderBy
    {
        get { throw new System.NotImplementedException(); }
    }

    public IAttributeDefinition ShortNameAttribute
    {
        get { throw new System.NotImplementedException(); }
    }

    public IAttributeDefinition NameAttribute
    {
        get { throw new System.NotImplementedException(); }
    }

    public IAttributeDefinition DescriptionAttribute
    {
        get { throw new System.NotImplementedException(); }
    }
}
