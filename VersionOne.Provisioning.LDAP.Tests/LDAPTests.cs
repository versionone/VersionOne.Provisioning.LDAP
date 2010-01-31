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
        
        [SetUp]      
        public void SetUp()
        {
            _ldapServerPath = ConfigurationManager.AppSettings["ldapServerPath"];
            _ldapGroupDN = ConfigurationManager.AppSettings["ldapGroupDN"];
            _ldapUsername = ConfigurationManager.AppSettings["ldapUsername"];
            _ldapPassword = ConfigurationManager.AppSettings["ldapPassword"];

            if (ConfigurationManager.AppSettings["useDefaultLDAPCredentials"].Trim().ToUpper() != "FALSE")
            {
                _useDefaultLDAPCredentials = true;
            }
        }
        
        [Test]
        public void TestGetUsersFromLdap()
        {
            LDAPReader ldapReader = new LDAPReader();
            IList<LDAPUser> ldapUsers = new List<LDAPUser>();

          //  ldapUsers = ldapReader.GetUsersFromLdap("192.168.36.4", "OU=Sales,OU=Users V1,DC=corp,DC=versionone,DC=net","","");

            ldapUsers = ldapReader.GetUsersFromLdap(_ldapServerPath, _ldapGroupDN, _ldapUsername, _ldapPassword, ConfigurationManager.AppSettings["mapToV1Username"], ConfigurationManager.AppSettings["mapToV1Fullname"], ConfigurationManager.AppSettings["mapToV1Email"], ConfigurationManager.AppSettings["mapToV1Nickname"], _useDefaultLDAPCredentials);
             
            
            Assert.AreEqual(2,ldapUsers.Count);

            
          
        }

    }
}
