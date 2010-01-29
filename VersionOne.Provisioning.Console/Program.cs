using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Manager manager = CreateManager();

            AssetList v1Users = manager.GetVersionOneUsers();
            IList<User> ldapUsers = manager.BuildLdapUsersList(ConfigurationManager.AppSettings["ldapServerPath"],
                                       ConfigurationManager.AppSettings["ldapGroupDN"],
                                       ConfigurationManager.AppSettings["ldapUsername"], ConfigurationManager.AppSettings["ldapPasswword"]);
            IList<User> actionUsers = manager.CompareUsers(ldapUsers, v1Users);
            manager.UpdateVersionOne(actionUsers);
        }

        private static Manager CreateManager()
        {
            V1APIConnector metaConn = new V1APIConnector(ConfigurationManager.AppSettings["V1Instance"] + "/meta.v1/");
            V1APIConnector dataConn = new V1APIConnector(ConfigurationManager.AppSettings["V1Instance"] + "/rest-1.v1/", ConfigurationManager.AppSettings["V1InstanceUsername"], ConfigurationManager.AppSettings["V1InstancePassword"]);
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
