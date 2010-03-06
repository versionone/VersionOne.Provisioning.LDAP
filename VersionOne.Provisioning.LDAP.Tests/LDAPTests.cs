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
        private string _ldapServerPath;
        private string _ldapGroupDN;
        private string _ldapUsername;
        private string _ldapPassword;
        private bool _useDefaultLDAPCredentials;
        private string _ldapGroupMemberAttribute;

        [SetUp]      
        public void SetUp()
        {
            _ldapServerPath = ConfigurationManager.AppSettings["ldapServerPath"];
            _ldapGroupDN = ConfigurationManager.AppSettings["ldapGroupDN"];
            _ldapUsername = ConfigurationManager.AppSettings["ldapUsername"];
            _ldapPassword = ConfigurationManager.AppSettings["ldapPassword"];
            _ldapGroupMemberAttribute = ConfigurationManager.AppSettings["ldapGroupMemberAttribute"];

            if (ConfigurationManager.AppSettings["useDefaultLDAPCredentials"].Trim().ToUpper() != "FALSE")
            {
                _useDefaultLDAPCredentials = true;
            }
        }
        
        [Test]
        public void TestGetUsersFromLdap()
        {
            IUserDirectoryReader ldapReader = new LDAPReader(_ldapServerPath, _ldapGroupMemberAttribute, _ldapUsername, _ldapPassword, ConfigurationManager.AppSettings["mapToV1Username"], ConfigurationManager.AppSettings["mapToV1Fullname"], ConfigurationManager.AppSettings["mapToV1Email"], ConfigurationManager.AppSettings["mapToV1Nickname"], _useDefaultLDAPCredentials);

            IList<DirectoryUser> ldapUsers = ldapReader.GetUsers(_ldapGroupDN);
             
            
            Assert.AreEqual(2,ldapUsers.Count);

            
          
        }

    }
}
