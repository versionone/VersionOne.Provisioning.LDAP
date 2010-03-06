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
                IDictionary<string, User> ldapUsers = manager.GetDirectoryUsers(ConfigurationManager.AppSettings["ldapGroupDN"]);
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
            IUserDirectoryReader ldapReader = GetLdapReader();
            return new Manager(v1, smtpAdaptor, ldapReader);
        }

        public static IUserDirectoryReader GetLdapReader()
        {
            bool useDefaultLDAPCredentials = false;
            if (ConfigurationManager.AppSettings["useDefaultLDAPCredentials"].Trim().ToUpper() != "FALSE")
            {
                useDefaultLDAPCredentials = true;
            }
            return new LDAPReader(ConfigurationManager.AppSettings["ldapServerPath"], 
                                  ConfigurationManager.AppSettings["ldapGroupMemberAttribute"], ConfigurationManager.AppSettings["ldapUsername"], 
                                  ConfigurationManager.AppSettings["ldapPasswword"], ConfigurationManager.AppSettings["mapToV1Username"],
                                  ConfigurationManager.AppSettings["mapToV1Fullname"], ConfigurationManager.AppSettings["mapToV1Email"], 
                                  ConfigurationManager.AppSettings["mapToV1Nickname"], useDefaultLDAPCredentials);
        }
    }
}
