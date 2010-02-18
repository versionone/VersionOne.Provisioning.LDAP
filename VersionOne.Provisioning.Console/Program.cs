using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using VersionOne.SDK.APIClient;
using NLog;

namespace VersionOne.Provisioning.Console
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string _proxyServerUri = ConfigurationManager.AppSettings["proxyServerUri"];
        private static string _proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
        private static string _proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
        private static string _proxyDomain = ConfigurationManager.AppSettings["proxyDomain"];

        static void Main(string[] args)
        {
            try
            {
                Manager manager = CreateManager();

                if (ConfigurationManager.AppSettings["useDefaultLDAPCredentials"].Trim().ToUpper() != "FALSE")
                {
                    manager.UseDefaultLDAPCredentials = true;
                }

                manager.UsernameMapping = ConfigurationManager.AppSettings["mapToV1Username"];
                manager.FullnameMapping = ConfigurationManager.AppSettings["mapToV1Fullname"];
                manager.EmailMapping = ConfigurationManager.AppSettings["mapToV1Email"];
                manager.NicknameMapping = ConfigurationManager.AppSettings["mapToV1Nickname"];
                
                IList<User> v1Users = manager.GetVersionOneUsers();
                IList<User> ldapUsers = manager.BuildLdapUsersList(ConfigurationManager.AppSettings["ldapServerPath"],
                                           ConfigurationManager.AppSettings["ldapGroupDN"],
                                           ConfigurationManager.AppSettings["ldapUsername"], ConfigurationManager.AppSettings["ldapPasswword"]);
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
            string _V1Instance = ConfigurationManager.AppSettings["V1Instance"];
            string _V1Login = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string _V1Password = ConfigurationManager.AppSettings["V1InstancePassword"];
            
            IAPIConnector metaConn;
            IAPIConnector dataConn;

            //Added to work with a proxy
            if (!String.IsNullOrEmpty(_proxyServerUri))
            {
                WebProxyBuilder proxyBuilder = new WebProxyBuilder();
                WebProxy webProxy = proxyBuilder.Build(_proxyServerUri, _proxyUsername, _proxyPassword, _proxyDomain);
                metaConn = new V1APIConnector(_V1Instance + @"meta.v1/", webProxy);
                dataConn = new V1APIConnector(_V1Instance + @"rest-1.v1/", _V1Login, _V1Password, false, webProxy);
            }
            else
            {
                metaConn = new V1APIConnector(_V1Instance + @"meta.v1/");
                dataConn = new V1APIConnector(_V1Instance + @"rest-1.v1/", _V1Login, _V1Password);
            }
            
            IMetaModel metaModel = new MetaModel(metaConn);
            IServices services = new Services(metaModel, dataConn);
            UserNotificationEmail userNotificationEmail = new UserNotificationEmail
                                                              {
                                                                  AdminEmail =
                                                                      ConfigurationManager.AppSettings["adminEmail"],
                                                                  AdminFullName =
                                                                      ConfigurationManager.AppSettings["adminFullName"],
                                                                  Body =
                                                                      ReadFile(
                                                                      ConfigurationManager.AppSettings["userNotificationEmailBodyFilename"]),
                                                                  Subject =
                                                                      ConfigurationManager.AppSettings["userNotificationEmailSubject"],
                                                                  VersionOneUrl =
                                                                      ConfigurationManager.AppSettings["V1Instance"]
                                                              };
            AdminNotificationEmail adminNotificationEmail = new AdminNotificationEmail
                                                                {
                                                                    AdminEmail =
                                                                        ConfigurationManager.AppSettings["adminEmail"],
                                                                    Body =
                                                                        ReadFile(
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailBodyFilename"]),
                                                                    Subject =
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailSubject"],
                                                                    VersionOneUrl =
                                                                        ConfigurationManager.AppSettings["V1Instance"]
                                                                };
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["smtpEnableSSL"]);
            SmtpAdaptor smtpAdaptor = new SmtpAdaptor(userNotificationEmail, adminNotificationEmail, smtpClient);

            return new Manager(services, metaModel, ConfigurationManager.AppSettings["V1UserDefaultRole"], smtpAdaptor);
        }

        private static string ReadFile(string filename)
        {
            string s = "";
            using (StreamReader rdr = File.OpenText(filename))
            {
                s = rdr.ReadToEnd();
            }
            return s;
        }
    }
}
