using System.Collections.Specialized;

namespace VersionOne.Provisioning.Tests {
    public class SmtpAdapterStub : ISmtpAdapter {
        public int UserNotificationCount;
        public int AdminNotificationCount;
        public void SendUserNotification(string username, string password, string to) {
            UserNotificationCount++;
        }

        public void SendAdminNotification(StringCollection addedUsernames, 
                                          StringCollection reactivatedUsernames, 
                                          StringCollection deactivatedUsernames) {
            AdminNotificationCount++;
        }
    }
}
