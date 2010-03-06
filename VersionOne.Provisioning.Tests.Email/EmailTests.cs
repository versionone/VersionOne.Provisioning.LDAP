using System.Collections.Specialized;
using System.Configuration;
using System.Net.Mail;

namespace VersionOne.Provisioning.Tests.Email
{
    public class EmailTests
    {
        
        public static void Main(string[] args)
        {
            SmtpAdaptor smtpAdaptor = GetSmtpAdaptor();
            smtpAdaptor.SendAdminNotification(new StringCollection(){"addeduser1", "addeduser2"}, new StringCollection(), new StringCollection() );
            smtpAdaptor.SendAdminNotification(new StringCollection() { "addeduser1", "addeduser2" }, new StringCollection() { "reactivateduser1", "reactivateduser2" }, new StringCollection());
            smtpAdaptor.SendAdminNotification(new StringCollection() { "addeduser1", "addeduser2" }, new StringCollection() { "reactivateduser1", "reactivateduser2" }, new StringCollection() { "deactivateduser1", "deactivateduser2" });
            smtpAdaptor.SendAdminNotification(new StringCollection(), new StringCollection() { "reactivateduser1", "reactivateduser2" }, new StringCollection());
            smtpAdaptor.SendAdminNotification(new StringCollection(), new StringCollection() { "reactivateduser1", "reactivateduser2" }, new StringCollection() { "deactivateduser1", "deactivateduser2" });
            smtpAdaptor.SendAdminNotification(new StringCollection(), new StringCollection(), new StringCollection() { "deactivateduser1", "deactivateduser2" });
            smtpAdaptor.SendUserNotification("newuser", "newpassword", "maggie.bullington@versionone.com");
        }

        private static SmtpAdaptor GetSmtpAdaptor()
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
            smtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["smtpEnableSSL"]);
            return new SmtpAdaptor(userNotificationEmail, adminNotificationEmail, smtpClient);

        }

    }
}