using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Model
{
    public class Defect
    {
        //   "code": "DE0001",
        //"name": "This a Test Defect Created using REST API 2",
        //"p_description":"This a Test Defect Created using REST API 2"
        public string code;
        public string name;
        public string p_description;
        public string p_requirement;
        public int p_severity;
        public int p_priority;
        public int p_state;
        public string p_submittedby;
        public string p_owner;

    }
}
