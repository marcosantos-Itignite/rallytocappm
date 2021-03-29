using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Model
{
    public class User
    {

        public string Role;
        public string DisplayName;
        public string EmailAddress;
        public string FirstName;
        public string LastName;
        public string UserName;

        public static implicit operator Task<object>(User v)
        {
            throw new NotImplementedException();
        }
    }

    public class Team
    {

        public string resource;
        public bool isOpenForTimeEntry;
        public bool isActive;
        public bool isRole;
       
    }
}
