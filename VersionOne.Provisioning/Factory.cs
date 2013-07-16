using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using VersionOne.SDK.APIClient;
using NLog;

namespace VersionOne.Provisioning {
    public class Factory {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static V1Instance GetV1Instance() {
            string V1Instance = ConfigurationManager.AppSettings["V1Instance"];
            string V1Login = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string V1Password = ConfigurationManager.AppSettings["V1InstancePassword"];
            string proxyServerUri = ConfigurationManager.AppSettings["proxyServerUri"];
            string defaultRole = ConfigurationManager.AppSettings["V1UserDefaultRole"];
            string useIntegratedAuth = ConfigurationManager.AppSettings["IntegratedAuth"];

            if(!V1Instance.EndsWith("/"))
                V1Instance += "/";

            try {
                IAPIConnector metaConnector;
                IAPIConnector dataConnector;
                bool useIntegrated = useIntegratedAuth.Equals("true");
                logger.Info("Attaching to VersionOne at: " + V1Instance);
                
                if (!string.IsNullOrEmpty(proxyServerUri)) {
                    ProxyProvider proxyProvider = GetProxyProvider();
                    metaConnector = new V1APIConnector(V1Instance + @"meta.v1/", null, null, false, proxyProvider);
                    dataConnector = new V1APIConnector(V1Instance + @"rest-1.v1/", V1Login, V1Password, useIntegrated, proxyProvider);
                } else {
                    metaConnector = new V1APIConnector(V1Instance + @"meta.v1/");
                    dataConnector = new V1APIConnector(V1Instance + @"rest-1.v1/", V1Login, V1Password, useIntegrated);
                }

                IMetaModel metaModel = new MetaModel(metaConnector);
                IServices services = new Services(metaModel, dataConnector);
                return new V1Instance(services, metaModel, defaultRole);
            } catch(Exception ex) {
                logger.Error(ex.Message);
                throw ex;
            }
        }

        public static SmtpAdapter GetSmtpAdaptor() {
            UserNotificationEmail userNotificationEmail =
                new UserNotificationEmail {
                    AdminEmail = ConfigurationManager.AppSettings["adminEmail"],
                    AdminFullName = ConfigurationManager.AppSettings["adminFullName"],
                    Body = Utils.ReadFile(ConfigurationManager.AppSettings["userNotificationEmailBodyFilename"]),
                    Subject = ConfigurationManager.AppSettings["userNotificationEmailSubject"],
                    VersionOneUrl = ConfigurationManager.AppSettings["V1Instance"]
                };
            AdminNotificationEmail adminNotificationEmail =
                new AdminNotificationEmail {
                    AdminEmail = ConfigurationManager.AppSettings["adminEmail"],
                    BodyTemplate = Utils.ReadFile(ConfigurationManager.AppSettings[
                                                        "adminNotificationEmailBodyTemplateFilename"]),
                    AddedUsersSection = Utils.ReadFile(ConfigurationManager.AppSettings[
                                                        "adminNotificationEmailBodyNewUsersFilename"]),
                    ReactivatedUsersSection = Utils.ReadFile(ConfigurationManager.AppSettings[
                                                        "adminNotificationEmailBodyReactivatedUsersFilename"]),
                    DeactivatedUsersSection = Utils.ReadFile(ConfigurationManager.AppSettings[
                                                        "adminNotificationEmailBodyDeactivatedUsersFilename"]),
                    Subject = ConfigurationManager.AppSettings["adminNotificationEmailSubject"],
                    VersionOneUrl = ConfigurationManager.AppSettings["V1Instance"]
                };

            SmtpClient smtpClient = new SmtpClient();
            smtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["smtpEnableSSL"]);
            return new SmtpAdapter(userNotificationEmail, adminNotificationEmail, smtpClient);
        }

        public static void ValidateConfiguration() {
            bool validV1Config = CheckVersionOneSettings();
            bool validLDAPConfig = CheckLDAPSettings();

            if(!validV1Config || !validLDAPConfig) {
                throw new Exception("There are errors in the configuration. Please check the application log for more information");
            }
        }

        private static ProxyProvider GetProxyProvider() {
            string proxyServerUri = ConfigurationManager.AppSettings["proxyServerUri"];
            string proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
            string proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
            string proxyDomain = ConfigurationManager.AppSettings["proxyDomain"];

            return new ProxyProvider(new Uri(proxyServerUri), proxyUsername, proxyPassword, proxyDomain);
        }

        private static bool CheckLDAPSettings() {
            bool success = true;
            string[] values = {"ldapGroupMemberAttribute",  "ldapServerPath",   "ldapGroupDN",
                               "maptoV1Username",           "maptoV1Fullname",  "mapToV1Email",
                               "mapToV1Nickname",           "useDefaultLDAPCredentials" };
            
            foreach (string entry in values) {
                if (!CheckForEmptyValue(entry)) {
                    success = false;
                }
            }

            return success;
        }

        private static bool CheckVersionOneSettings() {
            bool success = true;
            logger.Info("Checking VersionOne Settings");

            if(!CheckForEmptyValue("V1UserDefaultRole") || !CheckConnectionValid()) {
                success = false;
            }

            return success;
        }

        public static bool CheckConnectionValid() {
            string proxyServerUri = ConfigurationManager.AppSettings["proxyServerUri"];

            bool success = true;
            string connectionAddress = ConfigurationManager.AppSettings["V1Instance"];
            string userName = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string userPassword = ConfigurationManager.AppSettings["V1InstancePassword"];
            bool useIntegrated = ConfigurationManager.AppSettings["IntegratedAuth"].Equals("true");
            
            V1ConnectionValidator connectionValidator;
            
            if(!string.IsNullOrEmpty(proxyServerUri)) {
                var proxyProvider = GetProxyProvider();
                connectionValidator = new V1ConnectionValidator(connectionAddress, userName, userPassword, useIntegrated, proxyProvider);
            } else {
                connectionValidator = new V1ConnectionValidator(connectionAddress, userName, userPassword, useIntegrated);
            }

            try {
                connectionValidator.Test();
            } catch (Exception ex) {
                success = false;
                logger.Error(ex.Message);
            }

            return success;
        }

        private static bool CheckForEmptyValue(string key) {
            if (ConfigurationManager.AppSettings[key].Length == 0) {
                logger.Error("Application Config error: " + key + " can not be empty");
                return false;
            }

            return true;
        }
    }
}