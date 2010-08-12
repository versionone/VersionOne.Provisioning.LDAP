using System.Collections.Generic;
using NUnit.Framework;
using System.Configuration;

namespace VersionOne.Provisioning.LDAP.Tests
{
    [TestFixture]
    [Ignore("Integrational tests would not run on build server.")]
    public class LDAPTests
    {
        [Test]
        public void TestGetUsersFromLdap()
        {
            IUserDirectoryReader ldapReader = new LDAPReader();
            ldapReader.Initialize(ConfigurationManager.AppSettings);
            IList<DirectoryUser> ldapUsers = ldapReader.GetUsers();
            Assert.AreEqual(4,ldapUsers.Count);
        }

    }
}
