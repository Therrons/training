using System;
using System.Collections.Generic;
using System.Text;

namespace fraud_poc_project_buss.Dto
{
    public class Logins
    {
        public long id { get; set; }
        public string UserserName { get; set; }
        public string PassWord { get; set; }
        public int RoleId { get; set; }
    }
}
