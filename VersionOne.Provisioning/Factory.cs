using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using VersionOne.SDK.APIClient;
using System.Collections.Generic;
using NLog;

namespace VersionOne.Provisioning
{
    public class Factory
    {
        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public static V1Instance GetV1Instance()
        {
            string V1Instance = ConfigurationManager.AppSettings["V1Instance"];
            string V1Login = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string V1Password = ConfigurationManager.AppSettings["V1InstancePassword"];
            string proxyServerUri = ConfigurationManager.AppSettings["proxyServerUri"];
            string proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
            string proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
            string proxyDomain = ConfigurationManager.AppSettings["proxyDomain"];
            string defaultRole = ConfigurationManager.AppSettings["V1UserDefaultRole"];
            string useIntegratedAuth = ConfigurationManager.AppSettings["IntegratedAuth"];
            
            IAPIConnector metaConn;
            IAPIConnector dataConn;
            bool useIntegrated = useIntegratedAuth.Equals("true");
            logger.Info("Attaching to version one at: " + V1Instance);
            //Added to work with a proxy
            if (!String.IsNullOrEmpty(proxyServerUri))
            {
                WebProxyBuilder proxyBuilder = new WebProxyBuilder();
                WebProxy webProxy = proxyBuilder.Build(proxyServerUri, proxyUsername, proxyPassword, proxyDomain);
                metaConn = new V1APIConnector(V1Instance + @"meta.v1/", webProxy);
                dataConn = new V1APIConnector(V1Instance + @"rest-1.v1/", V1Login, V1Password, useIntegrated, webProxy);
            }
            else
            {
                metaConn = new V1APIConnector(V1Instance + @"meta.v1/");
                dataConn = new V1APIConnector(V1Instance + @"rest-1.v1/", V1Login, V1Password,useIntegrated);
            }
            
            IMetaModel metaModel = new MetaModel(metaConn);
            IServices services = new Services(metaModel, dataConn);
            return new V1Instance(services, metaModel, defaultRole);
        }

        public static SmtpAdaptor GetSmtpAdaptor()
        {
            UserNotificationEmail userNotificationEmail = new UserNotificationEmail
                                                              {
                                                                  AdminEmail =
                                                                      ConfigurationManager.AppSettings["adminEmail"],
                                                                  AdminFullName =
                                                                      ConfigurationManager.AppSettings["adminFullName"],
                                                                  Body =
                                                                      Utils.ReadFile(
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
                                                                    BodyTemplate =
                                                                        Utils.ReadFile(
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailBodyTemplateFilename"]),
                                                                    AddedUsersSection =
                                                                        Utils.ReadFile(
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailBodyNewUsersFilename"]),
                                                                    ReactivatedUsersSection =
                                                                        Utils.ReadFile(
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailBodyReactivatedUsersFilename"]),
                                                                    DeactivatedUsersSection =
                                                                        Utils.ReadFile(
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailBodyDeactivatedUsersFilename"]),
                                                                    Subject =
                                                                        ConfigurationManager.AppSettings["adminNotificationEmailSubject"],
                                                                    VersionOneUrl =
                                                                        ConfigurationManager.AppSettings["V1Instance"]
                                                                };

            SmtpClient smtpClient = new SmtpClient();
            smtpClient.EnableSsl = Boolean.Parse(ConfigurationManager.AppSettings["smtpEnableSSL"]);
            return new SmtpAdaptor(userNotificationEmail, adminNotificationEmail, smtpClient);
        }

        public static void ValidateConfiguration()
        {
            bool validConfig = true;
            validConfig = CheckVersionOneSettings();
            validConfig = CheckLDAPSettings();

            if(!validConfig)
            {
                Exception error = new Exception("There are errors in the configuration.  Please check the application log for more information");
                throw error;
            }


        }

        private static bool CheckLDAPSettings()
        {
            bool success = true;
            string[] values = {"ldapGroupMemberAttribute",  "ldapServerPath",   "ldapGroupDN",
                               "ldapUsername",              "ldapPassword",     "maptoV1Username", 
                               "maptoV1Fullname",           "mapToV1Email",     "mapToV1Nickname",
                               "useDefaultLDAPCredentials" };
           foreach(string entry in values)
           {
               if(!checkForEmptyValue(entry))
                   success = false;
           }
        }

        private static bool CheckVersionOneSettings()
        {
            bool success = true;
            logger.Info("Checking that VersionOne Settings are present");

            if(!checkForEmptyValue("V1Instance"))
            {
                success = false;
            }
            if (!checkForEmptyValue("IntegratedAuth"))
            {
                success = false;
            }
            if (ConfigurationManager.AppSettings["IntegratedAuth"].Equals("false"))
            {
                if ((!checkForEmptyValue("V1InstancePassword")) || (!checkForEmptyValue("V1InstanceUserName")))
                {
                    success = false;
                }
            }
            if(!checkForEmptyValue("V1UserDefaultRole"))
            {
                success = false;
            }
            return success;


        }

        private static bool checkForEmptyValue(string key)
        {
            try
            {
                if(ConfigurationManager.AppSettings[key].Length == 0)
                {
                    logger.Error("Application Config error: " +key + " can not be empty");
                    return false;
                }

            }
            catch (Exception error)
            {
                
               logger.Error(error.Message);
            }
            return true;
        }



    }
}
