using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;
using Rhino.Mocks;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning.Tests {
    [TestFixture]
    public class ManagerTester {
        private readonly MockRepository mockRepository = new MockRepository();
        private readonly IMetaModel metaModelMock = new MockRepository().Stub<IMetaModel>();
        private readonly IServices servicesMock = new MockRepository().Stub<IServices>();

        private IUserDirectoryReader directoryReaderMock;
        private ISmtpAdapter smtpAdapterMock;
        private V1Instance v1Instance;
        private Manager managerMock;
        
        [SetUp]
        public void SetUp() {
            directoryReaderMock = mockRepository.CreateMock<IUserDirectoryReader>();
            smtpAdapterMock = mockRepository.CreateMock<ISmtpAdapter>();;
            v1Instance = new V1Instance(servicesMock, metaModelMock, "Role:4");
            managerMock = new Manager(v1Instance, smtpAdapterMock, directoryReaderMock);
        }

        [Test]
        public void GetEmptyDirectoryUsersTest() {
            Expect.Call(directoryReaderMock.GetUsers()).Return(new List<DirectoryUser>());

            mockRepository.ReplayAll();
            
            Assert.IsEmpty((ICollection) managerMock.GetDirectoryUsers().Keys);

            mockRepository.VerifyAll();
        }

        [Test]
        public void GetDirectoryUsersTest() {
            IList<DirectoryUser> directoryUsers = new List<DirectoryUser>();
            DirectoryUser dirUser = new DirectoryUser();
            string userName = "User";
            const string email = "test@mail.com";
            const string fullName = "TestUser";

            dirUser.Username = userName;
            dirUser.Email = email;
            dirUser.FullName = fullName;
            directoryUsers.Add(dirUser);
            Expect.Call(directoryReaderMock.GetUsers()).Return(directoryUsers);

            mockRepository.ReplayAll();
            IDictionary<string, User> users = managerMock.GetDirectoryUsers();

            if (ConfigurationManager.AppSettings["IntegratedAuth"].Equals("true")) {
                userName = "\\" + userName;
            }

            string userNameCI = userName.ToLowerInvariant();

            Assert.IsNotEmpty((ICollection) users.Keys);
            Assert.AreEqual(users.Keys.Count, 1);
            Assert.IsTrue(users.Keys.Contains(userNameCI));
            Assert.AreEqual(users[userNameCI].Email, email);
            Assert.AreEqual(users[userNameCI].FullName, fullName);

            mockRepository.VerifyAll();
        }
    }
}
