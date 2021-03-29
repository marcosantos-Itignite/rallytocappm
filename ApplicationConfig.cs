using System;
using System.Collections.Generic;
using System.Text;

namespace Integration
{
    public static class ApplicationConfig
    {
        public static string RallyUserName { get; set; }
        public static string RallyPassword { get; set; }
        public static string RallyUrl { get; set; }
        public static Boolean RallyallowSSO { get; set; }
        public static string RallyWorkspace { get; set; }

        public static string AuthenticateToken;
        public static string AuthenticateType;
        public static string ClientApiPPM;

        public static string PPMUserName { get; set; }
        public static string PPMPassword { get; set; }
        public static string PPMUrl { get; set; }
        public static string PPMallowSSO { get; set; }

        public static StateDefect StateDefects;
        public static string GetAllDefects;
        
        public static int DefectBackDay;
        public static int DefectForwardDay;
        public static string PPMResourceDefault;

        public static string RallyKeyExternalID;
        public static bool RumModuloTest = false;

        //public static Boolean CreateFeaturesOnProject;

        public static int IterationBackDay;
        public static int IterationForwardDay;

        public static string StateTasks;
        public static string CreationDate;
        public static int TaskPageSizeLimite;
        public static int LastUpdate;


    }

    public enum StateDefect
    {
        All = 0,
        Submitted = 1,
        Open = 2,
        Fixed = 3,
        Closed = 4,
     
    }


}
