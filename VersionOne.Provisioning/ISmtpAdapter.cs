using System.Collections.Specialized;

namespace VersionOne.Provisioning {
    public interface ISmtpAdapter {
        void SendUserNotification(string username, string password, string to);
        void SendAdminNotification(StringCollection addedUsernames, 
                                   StringCollection reactivatedUsernames, 
                                   StringCollection deactivatedUsernames);
    }
}
