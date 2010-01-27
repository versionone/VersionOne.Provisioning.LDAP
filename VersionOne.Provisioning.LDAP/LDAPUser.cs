using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VersionOne.Provisioning.LDAP
{
    public class LDAPUser
    {
        public string Username { get; set; }

        public string FullName { get; set; }

        public string Nickname { get; set; }

        public string Email { get; set; }
    }
}
