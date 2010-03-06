using System.Collections.Generic;
using System.Collections.Specialized;

namespace VersionOne.Provisioning
{
    public interface IUserDirectoryReader
    {
        IList<DirectoryUser> GetUsers();
        void Initialize(NameValueCollection appSettings);
    }
}