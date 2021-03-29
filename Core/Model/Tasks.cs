using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Model
{
    public class Tasks
    {
        public string code;
        public string name;
        public string startDate;
        public string finishDate;
        public byte status;
        public byte priority;
        public bool isTask;
        public bool isKey;
        public bool isMilestone;
        public string parentTask;
        public bool isOpenForTimeEntry;


        public bool isLocked;
        //public string taskOwner;
        // public string agileExternalID;
    }


    public class Assignment
    {
        public string resource;
        public string finishDate;
        public string startDate;
    }

    public class TasksRally
    {
        public int FeatureID;
        public string TaskID;
        public string TaskName;
        public string resourceID;
        public string ResourceName;
        public DateTime entriesDate;
        public decimal value;

    }
    public class UserAssing
    {
        public string EMail;
        public string ProjectID;
        public string FeatureID;

    }


}
