using System.Collections.Generic;

namespace VersionOne.Provisioning
{
    public interface IUserDirectoryReader
    {
        IList<DirectoryUser> GetUsers(string userPath);
    }
}