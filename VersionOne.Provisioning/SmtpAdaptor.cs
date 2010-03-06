using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Text;
using NLog;

namespace VersionOne.Provisioning
{
    public interface ISmtpAdaptor
    {
        void SendUserNotification(string username, string password, string to);
        void SendAdminNotification(StringCollection addedUsernames, StringCollection reactivatedUsernames, StringCollection deactivatedUsernames);
    }

    public class SmtpAdaptor : ISmtpAdaptor
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
            Send(userEmail.CreateMessage(username, password, to));
        }

        public void SendAdminNotification(StringCollection addedUsernames, StringCollection reactivatedUsernames, StringCollection deactivatedUsernames)
        {
            Send(adminEmail.CreateMessage(addedUsernames,reactivatedUsernames,deactivatedUsernames));
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

        public MailMessage CreateMessage(string username, string password, string to)
        {
            string body = string.Format(Body, Subject, VersionOneUrl, username, password, AdminFullName, AdminEmail);
            return new MailMessage(AdminEmail, to, Subject, body) { IsBodyHtml = true };
        }
    }

    public class AdminNotificationEmail
    {
        public string Subject { get; set; }
        public string BodyTemplate { get; set; }
        public string AdminEmail { get; set; }
        public string VersionOneUrl { get; set; }
        public string AddedUsersSection { get; set; }
        public string ReactivatedUsersSection { get; set; }
        public string DeactivatedUsersSection { get; set; }

        public MailMessage CreateMessage(StringCollection addedUsernames, StringCollection reactivatedUsernames, StringCollection deactivatedUsernames)
        {
            string template = string.Format(BodyTemplate, Subject, VersionOneUrl, GetSection(addedUsernames, AddedUsersSection), GetSection(reactivatedUsernames, ReactivatedUsersSection), GetSection(deactivatedUsernames, DeactivatedUsersSection));
            string body = GetBody(addedUsernames, reactivatedUsernames, deactivatedUsernames, template);
            return new MailMessage(AdminEmail, AdminEmail, Subject, body) { IsBodyHtml = true };
        }

        private string GetBody(StringCollection addedUsernames, StringCollection reactivatedUsernames, StringCollection deactivatedUsernames, string template)
        {
            string body = template.Replace("<!--added-->", Utils.GetCommaDelimitedList(addedUsernames));
            body = body.Replace("<!--reactivated-->", Utils.GetCommaDelimitedList(reactivatedUsernames));
            return body.Replace("<!--deactivated-->", Utils.GetCommaDelimitedList(deactivatedUsernames));
        }

        private static string GetSection(ICollection usernames, string section)
        {
            return (usernames.Count > 0) ? section : "";
        }
    }
}
