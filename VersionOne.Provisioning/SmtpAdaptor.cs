using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Text;
using NLog;

namespace VersionOne.Provisioning
{
    public class SmtpAdaptor
    {
        public AdminNotificationEmail AdminEmail { get; set; }
        private readonly UserNotificationEmail userEmail;
        private readonly AdminNotificationEmail adminEmail;
        private readonly SmtpClient smtpClient;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SmtpAdaptor(UserNotificationEmail userEmail, AdminNotificationEmail adminEmail)
        {
            this.adminEmail = adminEmail;
            this.userEmail = userEmail;
        }

        public SmtpAdaptor(UserNotificationEmail userEmail, AdminNotificationEmail adminEmail, SmtpClient smtpClient)
            : this(userEmail, adminEmail)
        {
            this.smtpClient = smtpClient;
        }

        public void SendUserNotification(string username, string password, string to)
        {
            string body = string.Format(userEmail.Body, userEmail.Subject, userEmail.VersionOneUrl, username, password,
                                        userEmail.AdminFullName, userEmail.AdminEmail);
            MailMessage message = new MailMessage(userEmail.AdminEmail, to, userEmail.Subject, body) { IsBodyHtml = true };
            Send(message);
        }

        public void SendAdminNotification(StringCollection addedUsernames, StringCollection deactivatedUsernames, StringCollection collection)
        {
            string body = string.Format(adminEmail.Body, adminEmail.Subject, userEmail.VersionOneUrl, GetCommaDelimitedList(addedUsernames), GetCommaDelimitedList(deactivatedUsernames), GetCommaDelimitedList(deactivatedUsernames));
            MailMessage message = new MailMessage(adminEmail.AdminEmail, adminEmail.AdminEmail, adminEmail.Subject, body) { IsBodyHtml = true };
            Send(message);
        }

        private static string GetCommaDelimitedList(StringCollection strings)
        {
            string[] stringArray = new string[strings.Count];
            strings.CopyTo(stringArray, 0);
            return string.Join(", ", stringArray);
        }

        private void Send(MailMessage message)
        {
            if(smtpClient != null) smtpClient.Send(message);
            else logger.Info("Send called with no SmtpClient defined. Message To: " + message.To + " Subject: " + message.Subject);
        }
    }

    public class UserNotificationEmail
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AdminEmail { get; set; }
        public string AdminFullName { get; set; }
        public string VersionOneUrl { get; set; }
    }

    public class AdminNotificationEmail
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AdminEmail { get; set; }
        public string VersionOneUrl { get; set; }
    }
}
