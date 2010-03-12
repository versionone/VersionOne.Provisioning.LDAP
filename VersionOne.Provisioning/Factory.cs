using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning
{
    public class Factory
    {
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
                if (useIntegrated)
                {
                    dataConn = new V1APIConnector(V1Instance + @"rest-1.v1/");
                }
                else
                {
                    dataConn = new V1APIConnector(V1Instance + @"rest-1.v1/", V1Login, V1Password);
                }
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
    }
}
