using System.Collections.Generic;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning.Tests
{
    public class TestUserFactory
    {
        private readonly IMetaModel model;
        private readonly IServices services;

        public TestUserFactory(V1Instance v1) {
            services = v1.Services;
            model = v1.Model;
        }

        public IDictionary<string, User> CreateTestV1Users() {
            IDictionary<string, User> testV1Users = new Dictionary<string, User>();
            testV1Users.Add("jim", CreateTestV1User("jim", true));
            testV1Users.Add("sam", CreateTestV1User("sam", false));
            testV1Users.Add("tom", CreateTestV1User("tom", false));
            testV1Users.Add("val", CreateTestV1User("val", true));

            return testV1Users;
        }

        public User CreateTestV1User(string username, bool isinactive) {
            User user = new User();
            user.Username = username;
            user.IsInactive = isinactive;
            user.CheckInactivate = !isinactive;
            user.V1MemberAsset = new Asset(new Oid(new TestAssetType(), 1234, 1));
            return user;
        }

        public IDictionary<string, User> CreateTestLdapUsers() {
            IDictionary<string, User> users = new Dictionary<string, User>();

            string[] testUsers = {"abe", "ben", "cam", "sam", "val"};

            for (int i = 0; i < testUsers.Length; i++) {
                string username = testUsers[i];
                User user = CreateTestUser(username);
                users.Add(user.Username, user);
            }
            return users;
        }


        public User CreateTestUser(string username) {
            User user = new User();
            user.Username = username;
            user.FullName = username + " " + username + "son";
            user.Nickname = username;
            user.Email = username + "@test.com";
            return user;
        }

        public User CreateUserToDeactivate(string username) {
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

        public User CreateUserToAdd(string username) {
            User user = CreateTestUser(username);
            user.Create = true;
            return user;

        }
    }
}
