using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VersionOne.Provisioning
{
    public static class V1Constants
    {
        public const string MEMBER = "Member";
        public const string USERNAME = MEMBER + ".Username";
        public const string ISINACTIVE = MEMBER + ".IsInactive";
        public const string INACTIVATE = MEMBER + ".Inactivate";
        public const string REACTIVATE = MEMBER + ".Reactivate";
        public const string DELETE = MEMBER + ".Delete";
        public const string PASSWORD = MEMBER + ".Password";
        public const string CHECKINACTIVATE = MEMBER + ".CheckInactivate";
        public const string DEFAULTADMINOID = MEMBER + ":20";

    }
}
