using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VersionOne.SDK.APIClient;

namespace VersionOne.Provisioning
{
    public class User
    {
        public string Username { get; set; }

        public string FullName { get; set; }

        public string Nickname { get; set; }

        public string Email { get; set; }

        public bool IsInactive { get; set; }  

        public bool Create { get; set; }

        public bool Delete { get; set; }

        public bool Deactivate { get; set; }

        public bool Reactivate { get; set; }

        public Asset V1MemberAsset {get; set;} //Oid is a property of the Asset, if needed
    }

  }
