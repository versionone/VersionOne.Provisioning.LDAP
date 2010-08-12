using System.Collections.Generic;
using System.Collections.Specialized;

namespace VersionOne.Provisioning.Tests {
    public class DirectoryReaderStub : IUserDirectoryReader {

        public IList<DirectoryUser> GetUsers() {
            IList<DirectoryUser> users = new List<DirectoryUser>();
            users.Add(CreateTestUser("superman"));
            users.Add(CreateTestUser("spiderman"));
            return users;
        }

        public void Initialize(NameValueCollection appSettings) {
            throw new System.NotImplementedException();
        }

        private DirectoryUser CreateTestUser(string username) {
            DirectoryUser user = new DirectoryUser();
            user.Username = username;
            user.FullName = username + " " + username + "son";
            user.Nickname = username;
            user.Email = username + "@test.com";
            return user;
        }
    }
}
