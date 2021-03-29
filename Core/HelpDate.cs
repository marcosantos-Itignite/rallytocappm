using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Core
{
    public static class HelpDate
    {
        public static void GetIntervalWeek(DateTime date, out DateTime startDate, out DateTime finishDate)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(-1);
            }
            DateTime pdate = date;//DateTime.Parse(date);
            startDate = pdate.AddDays(-(int)pdate.DayOfWeek + (int)DayOfWeek.Sunday + 1);
            finishDate = pdate.AddDays(-(int)pdate.DayOfWeek + 8);
        }
    }
}
