using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using VersionOne.Provisioning.LDAP;
using VersionOne.SDK.APIClient;
using NLog;

namespace VersionOne.Provisioning.Console
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                Manager manager = CreateManager();
                IDictionary<string, User> v1Users = manager.GetVersionOneUsers();
                IDictionary<string, User> ldapUsers = manager.GetDirectoryUsers();
                IList<User> actionUsers = manager.CompareUsers(ldapUsers, v1Users);
                manager.UpdateVersionOne(actionUsers);
           }
            catch(Exception ex)
            {
                logger.ErrorException("An error has occurred.",ex);

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
