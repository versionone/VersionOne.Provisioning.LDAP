using System;
using System.Collections.Generic;
using System.Configuration;
using VersionOne.Provisioning.LDAP;
using NLog;

namespace VersionOne.Provisioning.Console
{
    internal class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            try
            {
                Factory.ValidateConfiguration();
                Manager manager = CreateManager();
                IDictionary<string, User> v1Users = manager.GetVersionOneUsers();
                IDictionary<string, User> ldapUsers = manager.GetDirectoryUsers();
                IList<User> actionUsers = manager.CompareUsers(ldapUsers, v1Users);
                manager.UpdateVersionOne(actionUsers);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        private static Manager CreateManager()
        {
            V1Instance v1 = Factory.GetV1Instance();
            SmtpAdaptor smtpAdaptor = Factory.GetSmtpAdaptor();
            IUserDirectoryReader ldapReader = new LDAPReader();
            ldapReader.Initialize(ConfigurationManager.AppSettings);
            return new Manager(v1, smtpAdaptor, ldapReader);
        }
    }
}