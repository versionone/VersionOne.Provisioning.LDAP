using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Configuration;

namespace VersionOne.Provisioning.LDAP.Tests
{
    [TestFixture]
    public class LDAPTests
    {
        
        [Test]
        public void TestGetUsersFromLdap()
        {
            IUserDirectoryReader ldapReader = new LDAPReader();
            ldapReader.Initialize(ConfigurationManager.AppSettings);
            IList<DirectoryUser> ldapUsers = ldapReader.GetUsers();
            Assert.AreEqual(2,ldapUsers.Count);
        }

    }
}
