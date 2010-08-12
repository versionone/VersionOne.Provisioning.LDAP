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
            string proxyUsername = ConfigurationManager.AppSettings["proxyUsername"];
            string proxyPassword = ConfigurationManager.AppSettings["proxyPassword"];
            string proxyDomain = ConfigurationManager.AppSettings["proxyDomain"];
            string defaultRole = ConfigurationManager.AppSettings["V1UserDefaultRole"];
            string useIntegratedAuth = ConfigurationManager.AppSettings["IntegratedAuth"];

            try {
                IAPIConnector metaConn;
                IAPIConnector dataConn;
                bool useIntegrated = useIntegratedAuth.Equals("true");
                logger.Info("Attaching to version one at: " + V1Instance);
                //Added to work with a proxy
                if (!String.IsNullOrEmpty(proxyServerUri)) {
                    WebProxyBuilder proxyBuilder = new WebProxyBuilder();
                    WebProxy webProxy = proxyBuilder.Build(proxyServerUri, proxyUsername, proxyPassword, proxyDomain);
                    metaConn = new V1APIConnector(V1Instance + @"meta.v1/", webProxy);
                    dataConn = new V1APIConnector(V1Instance + @"rest-1.v1/", 
                                                  V1Login, V1Password, useIntegrated, webProxy);
                } else {
                    metaConn = new V1APIConnector(V1Instance + @"meta.v1/");
                    dataConn = new V1APIConnector(V1Instance + @"rest-1.v1/", 
                                                  V1Login, V1Password, useIntegrated);
                }

                IMetaModel metaModel = new MetaModel(metaConn);
                IServices services = new Services(metaModel, dataConn);
                return new V1Instance(services, metaModel, defaultRole);
            } catch (Exception error) {
                logger.Error(error.Message);
                throw (error);
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
            smtpClient.EnableSsl = Boolean.Parse(ConfigurationManager.AppSettings["smtpEnableSSL"]);
            return new SmtpAdapter(userNotificationEmail, adminNotificationEmail, smtpClient);
        }

        public static void ValidateConfiguration() {
            bool validV1Config = CheckVersionOneSettings();
            bool validLDAPConfig = CheckLDAPSettings();

            if ((!validV1Config) || (!validLDAPConfig)) {
                Exception error = new Exception("There are errors in the configuration. "
                                                +" Please check the application log for more information");
                throw error;
            }


        }

        private static bool CheckLDAPSettings() {
            bool success = true;
            string[] values = {"ldapGroupMemberAttribute",  "ldapServerPath",   "ldapGroupDN",
                               "maptoV1Username",           "maptoV1Fullname",  "mapToV1Email",
                               "mapToV1Nickname",           "useDefaultLDAPCredentials" };
            foreach (string entry in values) {
                if (!CheckForEmptyValue(entry))
                    success = false;
            }
            return success;
        }

        private static bool CheckVersionOneSettings() {
            bool success = true;
            logger.Info("Checking VersionOne Settings");

            if (!CheckForEmptyValue("V1UserDefaultRole")) {
                success = false;
            }
            if (!CheckConnectionValid()) {
                success = false;
            }
            return success;

        }

        public static bool CheckConnectionValid() {
            bool success = true;
            string connectionAddress = ConfigurationManager.AppSettings["V1Instance"];
            string userName = ConfigurationManager.AppSettings["V1InstanceUsername"];
            string userPassword = ConfigurationManager.AppSettings["V1InstancePassword"];
            bool useIntegrated = ConfigurationManager.AppSettings["IntegratedAuth"].Equals("true");
            V1ConnectionValidator connectionValidator = 
                new V1ConnectionValidator(connectionAddress, userName, userPassword, useIntegrated);
            try {
                connectionValidator.Test();

            } catch (Exception error) {

                success = false;
                logger.Error(error.Message);
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
