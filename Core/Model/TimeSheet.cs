using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Model
{
    public class TimeSheet
    {
        public TimeSheet()
        {
            actuals = new actuals();
        }
        public int taskId { get; set; }
        public actuals actuals { get; set; }
    }
    public class actuals
    {
        public actuals()
        {
            segmentList = new segmentList();
        }
        public segmentList segmentList { get; set; }

    }
    public class segmentList
    {
        public segmentList()
        {
            segments = new List<segments>();
        }
        public List<segments> segments { get; set; }
    }
    public class segments
    {
        public DateTime start { get; set; }
        public DateTime finish { get; set; }
        public decimal value { get; set; }
    }
}
