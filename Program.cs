using ItigniteRallyIntegration;
using ItigniteRallyIntegration.Core;
using ItigniteRallyIntegration.Model;
using Newtonsoft.Json;
using NLog;
using Rally.RestApi;
using Rally.RestApi.Json;
using Rally.RestApi.Response;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Integration
{
    class Program
    {
        static RallyRestApi restApi;
        static RallyRestApi restApiPPM;
        static string workspaceRef;
        static HttpClient client;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {

                GetFileConfiguration();
                AuthenticateRally();
                if (ApplicationConfig.AuthenticateType == "Basic")
                {
                    AuthenticateBasicPPM();
                }
                else
                {
                    AuthenticateTokenPPM();
                }

                // Start a task - calling an async function
                Task<string> callTask = Task.Run(() => GetListProjectToSync());
                // Wait for it to finish
                callTask.Wait();
                // Get the result
                string astr = callTask.Result;
                // Write it our
                logger.Warn(astr);
            }
            catch (Exception e)
            {
                logger.Error(e, "An unexpected exception has occured");
            }
        }

        internal static void GetFileConfiguration()
        {
            logger.Info("Reading file configuration");

            try
            {
                ApplicationConfig.RallyUserName = ConfigurationManager.AppSettings["RallyUserName"];
                ApplicationConfig.RallyPassword = ConfigurationManager.AppSettings["RallyPassword"];
                ApplicationConfig.RallyUrl = ConfigurationManager.AppSettings["RallyUrl"];
                ApplicationConfig.RallyallowSSO = bool.Parse(ConfigurationManager.AppSettings["RallyallowSSO"]);
                ApplicationConfig.RallyWorkspace = ConfigurationManager.AppSettings["RallyWorkspace"];

                ApplicationConfig.PPMUserName = ConfigurationManager.AppSettings["PPMUserName"];
                ApplicationConfig.PPMPassword = ConfigurationManager.AppSettings["PPMPassword"];
                ApplicationConfig.PPMUrl = ConfigurationManager.AppSettings["PPMUrl"];
                ApplicationConfig.PPMallowSSO = ConfigurationManager.AppSettings["PPMallowSSO"];

                ApplicationConfig.AuthenticateToken = ConfigurationManager.AppSettings["AuthenticateToken"];
                ApplicationConfig.AuthenticateType = ConfigurationManager.AppSettings["AuthenticateType"];
                ApplicationConfig.ClientApiPPM = ConfigurationManager.AppSettings["ClientApiPPM"];



                ApplicationConfig.StateDefects = (StateDefect)Enum.Parse(typeof(StateDefect), ConfigurationManager.AppSettings["StateDefects"], true);
                ApplicationConfig.DefectBackDay = int.Parse(ConfigurationManager.AppSettings["DefectBackDay"]);
                ApplicationConfig.DefectForwardDay = int.Parse(ConfigurationManager.AppSettings["DefectForwardDay"]);
                ApplicationConfig.GetAllDefects = ConfigurationManager.AppSettings["GetAllDefects"];

                ApplicationConfig.PPMResourceDefault = ConfigurationManager.AppSettings["PPMResourceDefault"];
                //ApplicationConfig.CreateFeaturesOnProject = bool.Parse(ConfigurationManager.AppSettings["CreateFeaturesOnProject"]);

                ApplicationConfig.IterationBackDay = int.Parse(ConfigurationManager.AppSettings["IterationBackDay"]);
                ApplicationConfig.IterationForwardDay = int.Parse(ConfigurationManager.AppSettings["IterationForwardDay"]);

                //ApplicationConfig.StateTasks = ConfigurationManager.AppSettings["StateTasks"];
                //ApplicationConfig.CreationDate = ConfigurationManager.AppSettings["CreationDate"];
                ApplicationConfig.TaskPageSizeLimite = int.Parse(ConfigurationManager.AppSettings["TaskPageSizeLimite"]);

                ApplicationConfig.LastUpdate = int.Parse(ConfigurationManager.AppSettings["LastUpdate"]);

                ApplicationConfig.RallyKeyExternalID = ConfigurationManager.AppSettings["RallyKeyExternalID"];
                ApplicationConfig.RumModuloTest = bool.Parse(ConfigurationManager.AppSettings["RumModuloTest"]);



                workspaceRef = "/workspace/" + ApplicationConfig.RallyWorkspace;
                logger.Warn("****** Starting Sync Rally to PPM  ****** ");
                // Write details on the executing environment to the trace output.
                logger.Info("R2 - Starting integration between CA RAlly and CA PPM - " + DateTime.UtcNow);
                logger.Info("Operating system: " + System.Environment.OSVersion.ToString());
                logger.Info("Computer name: " + System.Environment.MachineName);
                logger.Info("User name: " + System.Environment.UserName);
                logger.Info("CLR runtime version: " + System.Environment.Version.ToString());
                logger.Info("Command line: " + System.Environment.CommandLine);

                logger.Warn("****** Environment variable ****** ");
                logger.Warn("RallyUserName:" + ApplicationConfig.RallyUserName);
                logger.Trace("RallyPassword:" + ApplicationConfig.RallyPassword);
                logger.Warn("RallyUrl:" + ApplicationConfig.RallyUrl);
                logger.Warn(ApplicationConfig.RallyallowSSO);
                logger.Warn("RallyWorkspace:" + ApplicationConfig.RallyWorkspace);

                logger.Warn("PPMUserName:" + ApplicationConfig.PPMUserName);
                logger.Trace("PPMPassword:" + ApplicationConfig.PPMPassword);
                logger.Warn("PPMUrl:" + ApplicationConfig.PPMUrl);
                logger.Warn("AuthenticateType:" + ApplicationConfig.AuthenticateType);
                logger.Warn("ClientApiPPM:" + ApplicationConfig.ClientApiPPM);


                logger.Warn("StateDefects:" + ApplicationConfig.StateDefects);
                logger.Warn("DefectBackDay:" + ApplicationConfig.DefectBackDay);
                logger.Warn("DefectForwardDay:" + ApplicationConfig.DefectForwardDay);
                logger.Warn(ApplicationConfig.PPMResourceDefault);

                logger.Warn("DefectForwardDay:" + ApplicationConfig.RallyKeyExternalID);
                logger.Warn(ApplicationConfig.RumModuloTest);


                //logger.Warn(ApplicationConfig.IterationBackDay);
                //logger.Warn(ApplicationConfig.IterationForwardDay);

                // logger.Warn(workspaceRef);

            }
            catch (Exception ex)
            {

                logger.Error("Error reading file configuration: " + ex.Message);
            }

        }
        internal static void AuthenticateRally()
        {
            logger.Info("Creating client Rally API");
            try
            {
                restApi = new RallyRestApi(webServiceVersion: "v2.0");
                dynamic authenticateUser = restApi.Authenticate(ApplicationConfig.RallyUserName,
                                                ApplicationConfig.RallyPassword,
                                                ApplicationConfig.RallyUrl,
                                                allowSSO: ApplicationConfig.RallyallowSSO);
            }
            catch (Exception ex)
            {

                logger.Error("Error creating Rally RespAPI - " + ex.Message);
            }

        }
        internal static void AuthenticateBasicPPM()
        {
            logger.Info("Creating client PPM API");
            try
            {
                client = new HttpClient();
                client.BaseAddress = new Uri(ApplicationConfig.PPMUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", ApplicationConfig.PPMUserName, ApplicationConfig.PPMPassword))));
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }
            catch (Exception ex)
            {

                logger.Error("Error creating PPM client - " + ex.Message);
            }

        }
        internal static void AuthenticateTokenPPM()
        {
            logger.Info("Creating client with Token PPM API");
            try
            {
                client = new HttpClient();
                client.BaseAddress = new Uri(ApplicationConfig.PPMUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization =
                             new AuthenticationHeaderValue("Bearer", ApplicationConfig.AuthenticateToken);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-ppm-client", ApplicationConfig.ClientApiPPM);
            }
            catch (Exception ex)
            {

                logger.Error("Error creating PPM client - " + ex.Message);
            }

        }



        internal static async Task<string> GetListProjectToSync()
        {
            //"rest/v1/projects?fields=agileExternalID%2CagileFormattedID%2CagileSync&filter=((isActive%20%3D%20true)%20and%20(isTemplate%20%3D%20false))

            logger.Warn("Get List of Project To Sync");
            //string path = ApplicationConfig.PPMUrl + "/rest/v1/projects?limit=500&fields=name%2CagileExternalID%2CagileFormattedID%2CagileSync&filter=((isActive%20%3D%20true)%20and%20(isTemplate%20%3D%20false))";

            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects?limit=500&filter=((isActive%20%3D%20true)%20and%20(isTemplate%20%3D%20false))";
           
            

            HttpResponseMessage response = new HttpResponseMessage();
            List<Project> projects = new List<Project>();
            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    //logger.Warn("List of Project: " + content._totalCount);
                    foreach (var item in content._results)
                    {
                        if (ApplicationConfig.RumModuloTest)
                        {
                            //"b9335475-bcb5-4c69-a604-19f91bffc770"
                            if (item.agileExternalID == ApplicationConfig.RallyKeyExternalID)
                            {

                                projects.Add(new Project
                                {
                                    name = item.name,
                                    _internalId = item._internalId,
                                    agileSync = item.agileSync,
                                    agileExternalID = item.agileExternalID,
                                    agileFormattedID = item.agileFormattedID

                                });
                                logger.Warn("Project Name: " + item.name + " CAPPM Id: " + item._internalId + " AgileSync: " + item.agileSync + " AgileExternalID: " + item.agileExternalID + " AgileFormattedID:" + item.agileFormattedID);
                                await GetProjectRally(Convert.ToString(item.agileExternalID), Convert.ToString(item._internalId));
                            }
                        }
                        else
                        {
                            projects.Add(new Project
                            {
                                name = item.name,
                                _internalId = item._internalId,
                                agileSync = item.agileSync,
                                agileExternalID = item.agileExternalID,
                                agileFormattedID = item.agileFormattedID

                            });
                            logger.Warn("Project Name: " + item.name + " CAPPM Id: " + item._internalId + " AgileSync: " + item.agileSync + " AgileExternalID: " + item.agileExternalID + " AgileFormattedID:" + item.agileFormattedID);
                            await GetProjectRally(Convert.ToString(item.agileExternalID), Convert.ToString(item._internalId));


                        }



                    }

                }
                else
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    logger.Error("Status: " + response.StatusCode.ToString() + "Json" + jsonString);

                }
            }
            catch (TaskCanceledException e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (Exception e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            return "************** Sync to complete ************** Date: " + DateTime.Now.ToString();
        }

        internal static async Task GetProjectRally(string ObjectUUID, string ProjectID)
        {
            try
            {
                Request request = new Request("PortfolioItem/Project");
                request.Workspace = workspaceRef;
                request.Fetch = new List<string>() { "Name", "ObjectID", "ObjectUUID", "FormattedID", "CreationDate",
                                                 "PlannedEndDate", "PlannedStartDate", "Children" };
                //request.Query = new Query("ObjectUUID", Query.Operator.Equals, ObjectUUID);
                QueryResult results = restApi.Query(request);

                foreach (var project in results.Results)
                {
                    //logger.Warn("Project name: " + project["Name"]);

                    Request moduloRequest = new Request(project["Children"]);
                    moduloRequest.Fetch = new List<string>() { "Name", "ObjectID", "ObjectUUID" };
                    QueryResult queryModuloResult = restApi.Query(moduloRequest);

                    logger.Warn("Modulo: " + queryModuloResult.TotalResultCount);

                    foreach (var modulo in queryModuloResult.Results)
                    {
                        logger.Warn("Modulo name: " + modulo["Name"]);
                        //logger.Warn("Modulo ObjectID: " + modulo["ObjectID"]);
                        //logger.Warn("Modulo ObjectUUID: " + modulo["_refObjectUUID"]);

                        await GetFeatureOnModulo(Convert.ToString(modulo["_refObjectUUID"]), ProjectID);

                    }

                }
            }
            catch (Exception e)
            {
                logger.Error("Error " + e.ToString());
            }

        }
        internal static async Task GetFeatureOnModulo(string ObjectUUID, string ProjectID)
        {
            Request request = new Request("PortfolioItem/Feature");
            request.Workspace = workspaceRef;
            request.Fetch = new List<string>() { "Name", "ObjectID", "ObjectUUID", "FormattedID", "CreationDate",
                                                 "PlannedEndDate", "PlannedStartDate", "UserStories","RevisionHistory" };
            request.Query = new Query("Parent.ObjectUUID", Query.Operator.Equals, ObjectUUID);
            QueryResult results = restApi.Query(request);

            //logger.Warn("Searching features between Planned Start: " + backString + " and Planned End:" + forwardString);
            logger.Trace("Feature Count:" + results.TotalResultCount);
            foreach (var feature in results.Results)
            {
                
                if (IsUpdateFeature(feature))
                {
                    //logger.Warn("Feature name: " + feature["Name"] + " / RallyID: " + feature["FormattedID"]);
                    var JsonReturnFeature = string.Empty;
                    string CodeFeature = Convert.ToString(feature["FormattedID"]);
                    await UpdateTimeSheetFeature(feature, ProjectID);
                }
            }
        }
        private static bool IsUpdateFeature(dynamic feature)
        {
            if (feature["RevisionHistory"] != null)
            {
                DateTime today = DateTime.Today;
                DateTime back = today.AddDays(ApplicationConfig.LastUpdate);
                String backString = back.ToString("yyyy-MM-dd");

                try
                {
                    String historyRef = feature["RevisionHistory"]._ref;
                    Request revRequest = new Request("Revision");
                    revRequest.Workspace = workspaceRef;
                    revRequest.Query = new Query("RevisionHistory", Query.Operator.Equals, historyRef)
                                     .And(new Query("CreationDate", Query.Operator.GreaterThan, backString));
                    revRequest.Fetch = new List<string>() { "User", "Description", "RevisionNumber", "CreationDate" };
                    QueryResult revisionsResults = restApi.Query(revRequest);
                    //logger.Error("queryRevisionHistoryRequestResult: " + revisionsResults.Results.Count());
                    logger.Warn("Feature name: " + feature["Name"] + " RallyID: " + feature["FormattedID"] + " CreationDate:" + feature["CreationDate"]);

                    if (revisionsResults.Results.Count() > 0)
                    {
                        return true;
                    }

                }
                catch (Exception e)
                {
                    logger.Error("Error " + e.ToString());
                }

            }

            return false;
        }

        private static bool IsUpdateStories(dynamic UserStories)
        {
            if (UserStories["RevisionHistory"] != null)
            {
                DateTime today = DateTime.Today;
                DateTime back = today.AddDays(ApplicationConfig.LastUpdate);
                String backString = back.ToString("yyyy-MM-dd");

                try
                {
                    String historyRef = UserStories["RevisionHistory"]._ref;
                    Request revRequest = new Request("Revision");
                    revRequest.Workspace = workspaceRef;
                    revRequest.Query = new Query("RevisionHistory", Query.Operator.Equals, historyRef)
                                     .And(new Query("CreationDate", Query.Operator.GreaterThan, backString));
                    revRequest.Fetch = new List<string>() { "User", "Description", "RevisionNumber", "CreationDate" };
                    QueryResult revisionsResults = restApi.Query(revRequest);
                    //logger.Error("queryRevisionHistoryRequestResult: " + revisionsResults.Results.Count());

                    if (revisionsResults.Results.Count() > 0)
                    {
                        return true;
                    }

                }
                catch (Exception e)
                {
                    logger.Error("Error " + e.ToString());
                }

            }

            return false;
        }
        internal static async Task UpdateTimeSheetFeature(dynamic Feature, string ProjectID)
        {
            string CodeFeature = Feature["FormattedID"].ToString();
            try
            {
                Tasks featureId = null;
                List<TasksRally> taskrally = new List<TasksRally>();
                List<TasksRally> defectrally = new List<TasksRally>();

                // List<UserAssing> userAssings = new List<UserAssing>();
                //userAssings.Add(new UserAssing { EMail = "", ProjectID = "0", FeatureID = "0" });

                ItigniteRallyIntegration.Services.TimeSheet clientTimesheet = new ItigniteRallyIntegration.Services.TimeSheet(client);


                Request UserStoriesRequest = new Request(Feature["UserStories"]);
                UserStoriesRequest.Fetch = new List<string>() {
                "Name", "ObjectID", "ObjectUUID", "Iteration","StartDate","EndDate", "Tasks", "Defects","RevisionHistory" };

                QueryResult queryUserStorieResult = restApi.Query(UserStoriesRequest);

                logger.Warn("UserStories: " + queryUserStorieResult.TotalResultCount);

                foreach (var s in queryUserStorieResult.Results)
                {

                    if (IsUpdateStories(s))
                    {
                        logger.Error("Stories name: " + s["Name"]);

                        Request tasksRequest = new Request(s["Tasks"]);
                        QueryResult queryTaskResult = restApi.Query(tasksRequest);
                        if (queryTaskResult.TotalResultCount > 0)
                        {
                            taskrally = await getTasks(ProjectID, CodeFeature, featureId, taskrally, queryTaskResult);

                        }


                        Request DefectsRequest = new Request(s["Defects"]);
                        QueryResult QueryDefectRequest = restApi.Query(DefectsRequest);

                        logger.Trace("Defects: " + QueryDefectRequest.TotalResultCount);

                        if (QueryDefectRequest.TotalResultCount > 0)
                        {
                            foreach (var item in QueryDefectRequest.Results)
                            {
                                await GetAndSyncDefectOnProject(ProjectID, item);
                                Request TasksDefectRequest = new Request(item["Tasks"]);
                                QueryResult QueryTasksDefectRequest = restApi.Query(TasksDefectRequest);
                                if (QueryTasksDefectRequest.TotalResultCount > 0)
                                {
                                    taskrally = await getTasks(ProjectID, CodeFeature, featureId, taskrally, QueryTasksDefectRequest);
                                }
                            }

                        }

                    }




                }

             
                foreach (var item in taskrally)
                {
                    logger.Warn("**** Update TimeSheet on CA PPM, Task n: " + item.TaskID + " Date: " + item.entriesDate + " Total Hours:" + item.value.ToString());
                    // Get Interval Week Date task
                    HelpDate.GetIntervalWeek(item.entriesDate, out DateTime startDate, out DateTime finishDate);
                    // Get Period ID
                    var timePeriodId = await clientTimesheet.GetTimePeriods(startDate, finishDate);
                    logger.Warn("Period ID:" + timePeriodId);
                    HelperTimeSheetId rest;
                    if (timePeriodId > 0)
                    {
                        // TimeSheet entries
                        rest = await clientTimesheet.GetTimeSheets(timePeriodId, item.FeatureID, item.resourceID);

                        // put here logic IF NOT EXIST TIME
                        if (rest._timesheetsInternalId == 0)
                        {
                            //create timeSheet
                            var resttimesheetsInternalId = await clientTimesheet.PostTimeSheet(timePeriodId, item.resourceID);
                            rest._timesheetsInternalId = resttimesheetsInternalId;

                        }
                        if (rest._timeEntriesInternalId == 0)
                        {
                            //Add Task to timesheet
                            var restTimeEntriesInternalId = await clientTimesheet.PostTimeSheetEntries(rest._timesheetsInternalId, item.FeatureID);
                            rest._timeEntriesInternalId = restTimeEntriesInternalId;
                        }
                        if (rest._timeEntriesInternalId != 0)
                        {
                            List<segments> ListSegments = await clientTimesheet.SegmentsTimeSheetEntries(rest);
                            // Patch TimeSheet 
                            var TimeSheetEntriesId = await clientTimesheet.PatchTimeSheetEntries(rest, item.FeatureID, item, ListSegments);

                        }
                        else
                        {
                            logger.Error("Period StartDate: " + startDate.AddDays(-1).ToString() + " End: " + finishDate.AddDays(-1).ToString() + " Period not found or Posted/Open(Adjustment) Or User not configured ");
                        }

                    }

                    else
                        logger.Error("Period not found or Close");
                }


            }
            catch (Exception e)
            {
                logger.Error(e.Message.ToString());

            }
        }

        private static async Task<List<TasksRally>> getTasks(string ProjectID, string CodeFeature, Tasks featureId, List<TasksRally> taskrally, QueryResult queryTaskResult)
        {
            logger.Warn("Tasks: " + queryTaskResult.TotalResultCount);


            featureId = await GetFeatureIdPPM(ProjectID, CodeFeature);
            var _internal = "";
            decimal ActualValue = 0;

            if (featureId.code == null)
            {
                logger.Warn("Feature not found in CA PPM, please create feature for sync. - Code: "+ CodeFeature);
            }
            else
            {
                foreach (var t in queryTaskResult.Results)
                {
                    //logger.Warn("Task Name: " + t["Name"] + " Actuals:" + t["Actuals"] + " Owner:" + t["Owner"]._ref.ToString());
                    //decimal actual = decimal.Parse(Convert.ToString(t["Actuals"]));
                    if (t["Actuals"] != null)
                    {
                        string EmailAddress = "admin";
                        if (t["Owner"] != null)
                        {
                            string Owner = t["Owner"]._ref.ToString();
                            //logger.Info("Get Assing Task");
                            var OwnerRest = GetUser(Owner);
                            EmailAddress = OwnerRest.Result.EmailAddress;
                        }
                        logger.Warn("Task Name: " + t["Name"] + " Actuals:" + t["Actuals"] + " Owner:" + EmailAddress);


                        //if (!userAssings.Any(d => d.EMail == EmailAddress && d.ProjectID == ProjectID && d.FeatureID == featureId.code))
                        //{
                        //After get Tasks owner create a team
                        _internal = await GetUserIDPPM(EmailAddress);
                        // var r = await CreateTeamPPM(ProjectID, _internal.ToString());

                        // userAssings.Add(new UserAssing { EMail = EmailAddress, ProjectID = ProjectID, FeatureID = featureId.code });
                         var JsonReturnAssing = await AssingTasksPPMAsync(_internal, featureId.startDate, featureId.finishDate);
                         await AssingTaskToUserAsync(ProjectID, featureId.code, JsonReturnAssing);

                        //}

                        var taskObjectID = t["ObjectID"].ToString();

                        //Fill TIMESHEET
                        if (t["RevisionHistory"] != null && t["Actuals"] > 0)
                        {

                            try
                            {

                                String historyRef = t["RevisionHistory"]._ref;
                                Request revRequest = new Request("Revision");
                                revRequest.Workspace = workspaceRef;
                                revRequest.Query = new Query("RevisionHistory", Query.Operator.Equals, historyRef);
                                revRequest.Fetch = new List<string>() { "User", "Description", "RevisionNumber", "CreationDate" };


                                QueryResult revisionsResults = restApi.Query(revRequest);
                                logger.Trace("queryRevisionHistoryRequestResult: " + revisionsResults.Results.Count());
                                bool uniqueCase = false;
                                bool isOrginal = false;
                                foreach (var x in revisionsResults.Results)
                                {
                                    string sDescription = x["Description"];
                                    var CreationDate = DateTime.Parse(x["CreationDate"].ToString()).ToString("yyyy-MM-dd");

                                    if (sDescription.Contains("Original revision") && (revisionsResults.Results.Count() == 1))
                                    {
                                        uniqueCase = true;
                                        isOrginal = true;
                                        var Actuals = Convert.ToString(t["Actuals"]);

                                        if (taskrally.Exists(d => d.entriesDate == DateTime.Parse(CreationDate)
                                                                                    && d.TaskID == featureId.code
                                                                                    && d.resourceID == _internal))
                                        {
                                            taskrally.Where(q => q.entriesDate == DateTime.Parse(CreationDate)
                                                                    && q.TaskID == featureId.code
                                                                    && q.resourceID == _internal).ToList()
                                                            .ForEach(b => b.value += decimal.Parse(Actuals, CultureInfo.InvariantCulture));



                                        }
                                        else
                                        {

                                            taskrally.Add(new TasksRally
                                            {
                                                FeatureID = int.Parse(featureId.code),
                                                TaskID = featureId.code,
                                                TaskName = t["Name"],
                                                resourceID = _internal,
                                                entriesDate = DateTime.Parse(CreationDate),
                                                value = decimal.Parse(Actuals, CultureInfo.InvariantCulture)
                                            });
                                        }

                                    }
                                    if (sDescription.Contains("ACTUALS added"))
                                    {
                                        uniqueCase = true;
                                        int start = sDescription.IndexOf("[") + 1;
                                        int length = sDescription.IndexOf("Hours") - start;
                                        sDescription = sDescription.Substring(start, length).TrimEnd();
                                        logger.Info("Date: " + CreationDate + " Hours:" + sDescription);
                                        //Add information about task
                                        if (taskrally.Exists(d => d.entriesDate == DateTime.Parse(CreationDate)
                                                                                    && d.TaskID == featureId.code
                                                                                    && d.resourceID == _internal))
                                        {

                                            taskrally.Where(q => q.entriesDate == DateTime.Parse(CreationDate)
                                                                                       && q.TaskID == featureId.code
                                                                                       && q.resourceID == _internal).ToList()
                                                                                       .ForEach(b => b.value += decimal.Parse(sDescription, CultureInfo.InvariantCulture));


                                        }
                                        else
                                        {

                                            taskrally.Add(new TasksRally
                                            {
                                                FeatureID = int.Parse(featureId.code),
                                                TaskID = featureId.code,
                                                TaskName = t["Name"],
                                                resourceID = _internal,
                                                entriesDate = DateTime.Parse(CreationDate),
                                                value = decimal.Parse(sDescription, CultureInfo.InvariantCulture)

                                            });
                                            isOrginal = true;
                                        }


                                    }
                                    if (sDescription.Contains("ACTUALS changed from"))
                                    {
                                        uniqueCase = true;
                                        int start = sDescription.IndexOf("[") + 1;
                                        int length = sDescription.IndexOf("Hours") - start;
                                        string BeforeActual = sDescription.Substring(start, length).TrimEnd();
                                        // ActualValue = decimal.Parse(BeforeActual);

                                        //]sruoH 0.5[ ot ]sruoH 0.4[ morf degnahc SLAUTCA;
                                        sDescription = new string(sDescription.Reverse().ToArray());
                                        start = sDescription.IndexOf("H") + 1;
                                        length = sDescription.IndexOf("[") - start;
                                        sDescription = sDescription.Substring(start, length).TrimEnd();
                                        sDescription = new string(sDescription.Reverse().ToArray()).Trim();
                                        ActualValue = decimal.Parse(BeforeActual, CultureInfo.InvariantCulture);

                                        logger.Info("Date: " + CreationDate + " Hours:" + sDescription);
                                        if (taskrally.Exists(d => d.entriesDate == DateTime.Parse(CreationDate)
                                                                                    && d.TaskID == featureId.code
                                                                                    && d.resourceID == _internal))
                                        {
                                            taskrally.Where(q => q.entriesDate == DateTime.Parse(CreationDate)
                                                                && q.TaskID == featureId.code
                                                                && q.resourceID == _internal).ToList()
                                                        .ForEach(b => b.value += (decimal.Parse(sDescription, CultureInfo.InvariantCulture) - ActualValue));
                                            isOrginal = true;
                                        }
                                        else
                                        {
                                            if (isOrginal)
                                            {
                                                taskrally.Add(new TasksRally
                                                {
                                                    FeatureID = int.Parse(featureId.code),
                                                    TaskID = featureId.code,
                                                    TaskName = t["Name"],
                                                    resourceID = _internal,
                                                    entriesDate = DateTime.Parse(CreationDate),
                                                    value = decimal.Parse(sDescription, CultureInfo.InvariantCulture) - ActualValue
                                                });
                                            }
                                            else
                                            {
                                                taskrally.Add(new TasksRally
                                                {
                                                    FeatureID = int.Parse(featureId.code),
                                                    TaskID = featureId.code,
                                                    TaskName = t["Name"],
                                                    resourceID = _internal,
                                                    entriesDate = DateTime.Parse(CreationDate),
                                                    value = decimal.Parse(sDescription, CultureInfo.InvariantCulture)- ActualValue
                                                });
                                            }

                                        }

                                    }



                                }
                                if (!uniqueCase)
                                {
                                    var Actuals = Convert.ToString(t["Actuals"]);
                                    taskrally.Add(new TasksRally
                                    {
                                        FeatureID = int.Parse(featureId.code),
                                        TaskID = featureId.code,
                                        TaskName = t["Name"],
                                        resourceID = _internal,
                                        entriesDate = DateTime.Parse(t["CreationDate"]),
                                        value = decimal.Parse(Actuals, CultureInfo.InvariantCulture)
                                    });
                                }



                            }
                            catch (Exception e)
                            {
                                logger.Error("Error " + e.ToString());
                            }

                        }


                    }
                }


            }






            return taskrally;
        }

        internal static async Task<Tasks> GetFeatureIdPPM(string IdProject, string AgileExternalID)
        {
            logger.Info("Get Feature ID");

            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/tasks?fields=startDate%2CfinishDate&filter=(agileTaskFormattedID%20%3D%20'" + AgileExternalID + "')";

            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {

                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (content._totalCount > 0)
                    {
                        
                        Tasks task = new Tasks();
                        task.code = content._results[0]._internalId;
                        task.startDate =  content._results[0].startDate.ToString("yyyy-MM-ddTHH:mm:ss");
                        task.finishDate = content._results[0].finishDate.ToString("yyyy-MM-ddTHH:mm:ss");
                        return task;
                    }



                }
                else
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    logger.Error("Status: " + response.StatusCode.ToString() + "Json" + jsonString);

                }
            }
            catch (TaskCanceledException e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (Exception e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            return new Tasks();
        }


        //internal static async Task GetFeatureOnProject(string ObjectID, string ProjectID, string ProjectName, string CreationDateProject)
        //{

        //    DateTime today = DateTime.Today;
        //    DateTime forward = today.AddDays(90);
        //    String forwardString = forward.ToString("yyyy-MM-dd");
        //    DateTime back = today.AddDays(-60);
        //    String backString = back.ToString("yyyy-MM-dd");
        //    String projectRef = "/project/" + ObjectID;

        //    Request request = new Request("PortfolioItem/Feature");
        //    request.Workspace = workspaceRef;
        //    request.Fetch = new List<string>() { "Name", "ObjectID", "FormattedID", "LastUpdateDate", "PlannedStartDate", "PlannedEndDate", "Project", "UserStories" };
        //    request.Query = new Query("PlannedStartDate", Query.Operator.GreaterThan, backString)
        //                        .And(new Query("PlannedEndDate", Query.Operator.LessThanOrEqualTo, forwardString)
        //                        .And(new Query("Parent.ObjectUUID", Query.Operator.Equals, ObjectID)));
        //    QueryResult results = restApi.Query(request);

        //    foreach (var f in results.Results)
        //    {
        //        //logger.Warn("Name: " + f["Name"] + " PlannedStartDate: " + f["PlannedStartDate"] + " PlannedEndDate: " + f["PlannedEndDate"] + " Project: " + f["Project"]._refObjectName);



        //        string CodeFeature = f["FormattedID"].ToString();
        //        var codeFeature = await IsUpdateUserStories(ProjectID, CodeFeature);
        //        var JsonReturnFeature = string.Empty;

        //        if (codeFeature.ToString() != "0")
        //        {
        //            string codeUserStories = codeFeature.ToString();
        //            JsonReturnFeature = await CreateUserStoriesKeyTask(f, null, true, CreationDateProject);
        //            await UpdateUserStories(ProjectID, codeUserStories, JsonReturnFeature);
        //        }
        //        else
        //        {
        //            JsonReturnFeature = await CreateUserStoriesKeyTask(f, null, false, CreationDateProject);
        //            codeFeature = await CreateUserStories(ProjectID, JsonReturnFeature);
        //        }

        //        Request UserStoriesRequest = new Request(f["UserStories"]);
        //        UserStoriesRequest.Fetch = new List<string>()
        //        {
        //            "Name",
        //            "ObjectID",
        //            "ScheduleState",
        //            "State",
        //            "FormattedID",
        //            "CreationDate",
        //            "ReleaseDate",
        //            "PlanEstimate",
        //            "Iteration",
        //            "StartDate",
        //            "EndDate",
        //            "Release",
        //            "ScheduleState",

        //            "Tasks",
        //        };
        //        QueryResult queryUserStorieResult = restApi.Query(UserStoriesRequest);
        //        logger.Warn("UserStories: " + queryUserStorieResult.TotalResultCount);

        //        foreach (var s in queryUserStorieResult.Results)
        //        {
        //            logger.Warn("Stories name: " + s["Name"]);


        //            string CodeUserStories = s["FormattedID"].ToString();
        //            var codeUserStoriesKeyTask = await IsUpdateUserStories(ProjectID, CodeUserStories);
        //            var JsonReturn = string.Empty;
        //            if (codeUserStoriesKeyTask.ToString() != "0")
        //            {
        //                string codeUserStories = codeUserStoriesKeyTask.ToString();
        //                JsonReturn = await CreateUserStoriesKeyTask(s, codeFeature, true, CreationDateProject);
        //                await UpdateUserStories(ProjectID, codeUserStories, JsonReturn);
        //            }
        //            else
        //            {
        //                JsonReturn = await CreateUserStoriesKeyTask(s, codeFeature, false, CreationDateProject);
        //                codeUserStoriesKeyTask = await CreateUserStories(ProjectID, JsonReturn);
        //            }

        //            Request tasksRequest = new Request(s["Tasks"]);

        //            QueryResult queryTaskResult = restApi.Query(tasksRequest);
        //            logger.Warn("Tasks: " + queryTaskResult.TotalResultCount);

        //            List<TasksRally> taskrally = new List<TasksRally>();
        //            ItigniteRallyIntegration.Services.TimeSheet clientTimesheet = new ItigniteRallyIntegration.Services.TimeSheet(client);
        //            decimal ActualValue = 0;
        //            foreach (var t in queryTaskResult.Results)
        //            {
        //                logger.Warn("Task Name: " + t["Name"] + " Actuals:" + t["Actuals"] + " ToDo:" + t["ToDo"] + " Estimate:" + t["Estimate"]);

        //                string EmailAddress = "admin";
        //                if (t["Owner"] != null)
        //                {
        //                    string Owner = t["Owner"]._ref.ToString();
        //                    logger.Info("Get Assing Task");
        //                    var OwnerRest = GetUser(Owner);
        //                    EmailAddress = OwnerRest.Result.EmailAddress;
        //                }

        //                //Create a team
        //                var _internal = await GetUserIDPPM(EmailAddress);
        //                var r = await CreateTeamPPM(ProjectID, _internal.ToString());
        //                logger.Warn("Owner: " + r);

        //                //Assing Task
        //                var JsonReturnAssing = await AssingTasksPPMAsync(t, s);
        //                await AssingTaskToUserAsync(ProjectID, codeUserStoriesKeyTask, JsonReturnAssing);

        //                //Fill TIMESHEET
        //                if (t["RevisionHistory"] != null && t["Actuals"] > 0)
        //                {

        //                    try
        //                    {
        //                        String historyRef = t["RevisionHistory"]._ref;
        //                        Request revRequest = new Request("Revision");
        //                        revRequest.Workspace = workspaceRef;
        //                        revRequest.Query = new Query("RevisionHistory", Query.Operator.Equals, historyRef);
        //                        revRequest.Fetch = new List<string>() { "User", "Description", "RevisionNumber", "CreationDate" };


        //                        QueryResult revisionsResults = restApi.Query(revRequest);
        //                        logger.Warn("queryRevisionHistoryRequestResult: " + revisionsResults.Results.Count());


        //                        foreach (var x in revisionsResults.Results)
        //                        {
        //                            string sDescription = x["Description"];
        //                            var CreationDate = DateTime.Parse(x["CreationDate"].ToString()).ToString("yyyy-MM-dd");


        //                            if (sDescription.Contains("ACTUALS added"))
        //                            {
        //                                int start = sDescription.IndexOf("[") + 1;
        //                                int length = sDescription.IndexOf("Hours") - start;
        //                                sDescription = sDescription.Substring(start, length).TrimEnd();
        //                                logger.Info("Date: " + CreationDate + " Hours:" + sDescription);
        //                                //Add information about task
        //                                if (taskrally.Exists(d => d.entriesDate == DateTime.Parse(CreationDate)))
        //                                {

        //                                    taskrally.Where(q => q.entriesDate == DateTime.Parse(CreationDate) &&
        //                                                              q.TaskID == int.Parse(codeUserStoriesKeyTask)).ToList()
        //                                                     .ForEach(b => b.value += decimal.Parse(sDescription));
        //                                }
        //                                else
        //                                {

        //                                    taskrally.Add(new TasksRally
        //                                    {
        //                                        TaskID = int.Parse(codeUserStoriesKeyTask),
        //                                        entriesDate = DateTime.Parse(CreationDate),
        //                                        value = decimal.Parse(sDescription)
        //                                    });
        //                                }


        //                            }
        //                            if (sDescription.Contains("ACTUALS changed from"))
        //                            {
        //                                int start = sDescription.IndexOf("[") + 1;
        //                                int length = sDescription.IndexOf("Hours") - start;
        //                                string BeforeActual = sDescription.Substring(start, length).TrimEnd();
        //                                ActualValue = decimal.Parse(BeforeActual);

        //                                //]sruoH 0.5[ ot ]sruoH 0.4[ morf degnahc SLAUTCA;
        //                                sDescription = new string(sDescription.Reverse().ToArray());
        //                                start = sDescription.IndexOf("H") + 1;
        //                                length = sDescription.IndexOf("[") - start;
        //                                sDescription = sDescription.Substring(start, length).TrimEnd();
        //                                sDescription = new string(sDescription.Reverse().ToArray()).Trim();

        //                                logger.Info("Date: " + CreationDate + " Hours:" + sDescription);
        //                                if (taskrally.Exists(d => d.entriesDate == DateTime.Parse(CreationDate)))
        //                                {
        //                                    taskrally.Where(q => q.entriesDate == DateTime.Parse(CreationDate) &&
        //                                                    q.TaskID == int.Parse(codeUserStoriesKeyTask)).ToList()
        //                                            .ForEach(b => b.value += (decimal.Parse(sDescription) - ActualValue));

        //                                }
        //                                else
        //                                {
        //                                    taskrally.Add(new TasksRally
        //                                    {
        //                                        TaskID = int.Parse(codeUserStoriesKeyTask),
        //                                        entriesDate = DateTime.Parse(CreationDate),
        //                                        value = decimal.Parse(sDescription) - ActualValue
        //                                    });
        //                                }

        //                            }


        //                        }


        //                    }
        //                    catch (Exception e)
        //                    {

        //                        throw;
        //                    }

        //                }




        //            }


        //            foreach (var item in taskrally)
        //            {
        //                logger.Warn("****Update TimeSheet on CA PPM, Task n: " + codeUserStoriesKeyTask + " Date: " + item.entriesDate + " Total Hours:" + item.value);
        //                // Get Interval Week Date task
        //                HelpDate.GetIntervalWeek(item.entriesDate, out DateTime startDate, out DateTime finishDate);
        //                // Get Period ID
        //                var timePeriodId = await clientTimesheet.GetTimePeriods(startDate, finishDate);
        //                HelperTimeSheetId rest;
        //                if (timePeriodId > 0)
        //                {
        //                    // TimeSheet entries
        //                    rest = await clientTimesheet.GetTimeSheets(timePeriodId, int.Parse(codeUserStoriesKeyTask));

        //                    // put here logic IF NOT EXIST TIME
        //                    if (rest._timesheetsInternalId == 0)
        //                    {
        //                        //create timeSheet
        //                        var resttimesheetsInternalId = await clientTimesheet.PostTimeSheet(timePeriodId);
        //                        rest._timesheetsInternalId = resttimesheetsInternalId;

        //                    }
        //                    if (rest._timeEntriesInternalId == 0)
        //                    {
        //                        //Add Task to timesheet
        //                        var restTimeEntriesInternalId = await clientTimesheet.PostTimeSheetEntries(rest._timesheetsInternalId, int.Parse(codeUserStoriesKeyTask));
        //                        rest._timeEntriesInternalId = restTimeEntriesInternalId;
        //                    }
        //                    //If _timeEntriesInternalId
        //                    if (rest._timeEntriesInternalId != 0)
        //                    {
        //                        List<segments> ListSegments = await clientTimesheet.SegmentsTimeSheetEntries(rest);
        //                        // Patch TimeSheet 
        //                        var TimeSheetEntriesId = await clientTimesheet.PatchTimeSheetEntries(rest, int.Parse(codeUserStoriesKeyTask), item, ListSegments);

        //                    }
        //                    else
        //                    {
        //                        logger.Error("Period StartDate: " + startDate.AddDays(-1).ToString() + " End: " + finishDate.AddDays(-1).ToString() + " not found or Posted/Open(Adjustment)");
        //                    }

        //                }

        //                else
        //                    logger.Error("Period not found or Close");
        //            }

        //        }







        //    }



        //}

        #region PROJECT PPM
        //internal static async Task CreateProject(string Json)
        //{
        //    logger.Info("Create Project");
        //    string path = ApplicationConfig.PPMUrl + "/rest/v1/projects";

        //    HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
        //    HttpResponseMessage response = new HttpResponseMessage();

        //    try
        //    {
        //        response = await client.PostAsync(path, content);
        //        string jsonString = response.Content.ReadAsStringAsync().Result;

        //        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        //        {
        //            logger.Error("Create Project - Status: " + response.StatusCode.ToString() + "Json" + jsonString);
        //        }
        //        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            logger.Error("Create Project - Status: " + response.StatusCode.ToString() + "Json" + jsonString);
        //        }
        //        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            logger.Error("Create Project: " + response.StatusCode.ToString());
        //        }
        //    }
        //    catch (HttpRequestException e)
        //    {
        //        logger.Error("Connection error with client PPM: " + e.InnerException.Message);
        //    }
        //    catch (TaskCanceledException e)
        //    {
        //        logger.Error("ERROR: " + e.ToString());
        //    }
        //}
        //internal static async Task UpdateProject(string IdProject, string Json)
        //{
        //    logger.Info("Update Project");
        //    string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject;

        //    var method = new HttpMethod("PATCH");

        //    HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");

        //    var request = new HttpRequestMessage(method, path)
        //    {
        //        Content = content
        //    };

        //    HttpResponseMessage response = new HttpResponseMessage();

        //    try
        //    {
        //        response = await client.SendAsync(request);
        //        string jsonString = response.Content.ReadAsStringAsync().Result;
        //        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        //        {
        //            logger.Error("Update Project: " + IdProject + " - Status: " + response.StatusCode.ToString() + "Json" + jsonString);
        //        }
        //        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            logger.Error("Update Project: " + IdProject + " - Status: " + response.StatusCode.ToString() + "Json" + jsonString);
        //        }
        //        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        //        {
        //            logger.Error("Update Project: " + IdProject + " : " + response.StatusCode.ToString());
        //        }
        //    }
        //    catch (HttpRequestException e)
        //    {
        //        logger.Error("Connection error with client PPM: " + e.InnerException.Message);
        //    }
        //    catch (TaskCanceledException e)
        //    {
        //        logger.Error("ERROR: " + e.ToString());
        //    }



        //}






        internal static async Task<string> IsUpdatePPMProject(string name)
        {
            logger.Info("IsUpdatePPMProject");
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects?fields=code%2C&filter=(isActive%20%3D%20true)%20and%20(name%20%3D%20'" + name + "')";
            string internalid = "0";

            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);

                    foreach (var item in content._results)
                    {

                        internalid = item._internalId;
                        //code = item.code;
                        //externalID = item.agileExternalID;
                        //logger.Info("Name: " + name + " _internalId: " + internalid + " Code: " + code + " ExternalID: " + externalID);
                    }

                }
                else
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    logger.Error("Status: " + response.StatusCode.ToString() + "Json" + jsonString);

                }
            }
            catch (TaskCanceledException e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (Exception e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            return internalid.ToString();
        }
        internal static async Task<string> IsUpdatePPMTask(string IdProject, string AgileExternalID)
        {
            logger.Info("IsUpdatePPMTask");

            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/tasks?filter=(agileExternalID%20%3D%20'" + AgileExternalID + "')";
            string internalid = "0";

            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    internalid = content._results[0]._internalId;
                }
                else
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    logger.Error("Status: " + response.StatusCode.ToString() + "Json" + jsonString);

                }
            }
            catch (TaskCanceledException e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (Exception e)
            {
                logger.Error("Error client PPM: " + e.ToString());
            }
            return internalid;
        }
        #endregion Project









        #region DEFECTS CAPPM
        internal static async Task GetAndSyncDefectOnProject(string ProjectId, dynamic Defect)
        {
            logger.Warn("Get and Sync Defect On Project");
            try
            {
                //DateTime today = DateTime.Today;
                //DateTime forward = today.AddDays(ApplicationConfig.DefectForwardDay);
                //String forwardString = forward.ToString("yyyy-MM-dd");
                //DateTime back = today.AddDays(ApplicationConfig.DefectBackDay);
                //String backString = back.ToString("yyyy-MM-dd");

                //String projectRef = "/project/" + ObjectID;

                //Request sRequest = new Request("Defect");
                //sRequest.Project = projectRef;
                //sRequest.Limit = 5000;
                //sRequest.Fetch = new List<string>()
                //{
                //    "Name","Description","FormattedID","ObjectID","CreationDate","State","ClosedDate","Priority",
                //    "Severity","LastUpdateDate","Environment","Owner","SubmittedBy","Requirement",
                //};

                //if (ApplicationConfig.StateDefects == StateDefect.Closed)
                //    sRequest.Query = new Query("(State = Closed)");
                //if (ApplicationConfig.StateDefects == StateDefect.Fixed)
                //    sRequest.Query = new Query("(State = Fixed)");
                //if (ApplicationConfig.StateDefects == StateDefect.Open)
                //    sRequest.Query = new Query("(State = Open)");
                //if (ApplicationConfig.StateDefects == StateDefect.Submitted)
                //    sRequest.Query = new Query("(State = Submitted)");

                //if (ApplicationConfig.GetAllDefects != "No")
                //{
                //    sRequest.Query = new Query("LastUpdateDate", Query.Operator.GreaterThan, backString);
                //}


                //QueryResult queryResults = restApi.Query(sRequest);

                //logger.Warn("Count Defects: " + queryResults.TotalResultCount);

                //foreach (var s in queryResults.Results)
                //{
                //    logger.Warn("FormattedID: " + s["FormattedID"] + " Name: " + s["Name"]);

                var code = "0";
                // IF code project = 0 is a new Defect, not necessary verirify 

                code = await IsUpdateDefect(ProjectId, Defect["FormattedID"].ToString());



                var jDefect = await CreateDefectPPM(Defect);

                if (code != "0")
                {
                    string x = code;
                    await UpdateDefectOnProject(ProjectId, jDefect, x);
                }
                else
                {
                    await CreateDefectOnProject(ProjectId, jDefect);
                }

                // }
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client Rally: " + e.InnerException.Message);
            }
            catch (Exception ex)
            {
                logger.Error("Error function GetAndSyncDefectOnProject - " + ex.Message);
            }



        }
        private static async Task<string> CreateDefectPPM(dynamic s)
        {
            logger.Info("Create Defect PPM");
            string jDefect = string.Empty;
            Defect def;
            try
            {
                def = new Defect();
                def.code = s["FormattedID"].ToString();
                def.name = StringExt.Truncate(s["Name"].ToString(), 80);
                def.p_description = StringExt.StripHTML(s["Description"].ToString());

                if (s["Requirement"] != null)
                {
                    def.p_requirement = StringExt.Truncate(s["Requirement"]._refObjectName.ToString(), 2000);
                }
                switch (s["Severity"].ToString())
                {
                    case "None":
                        def.p_severity = 0;
                        break;
                    case "1-Critical":
                        def.p_severity = 1;
                        break;
                    case "2-Major":
                        def.p_severity = 2;
                        break;
                    case "3-Minor":
                        def.p_severity = 3;
                        break;
                    case "4-Trivial":
                        def.p_severity = 4;
                        break;
                }
                switch (s["Priority"].ToString())
                {
                    case "None":
                        def.p_priority = 0;
                        break;
                    case "1-Emergency Fix":
                        def.p_priority = 1;
                        break;
                    case "2-Urgent":
                        def.p_priority = 2;
                        break;
                    case "3-High":
                        def.p_priority = 3;
                        break;
                    case "4-Medium":
                        def.p_priority = 4;
                        break;
                    case "5-Low":
                        def.p_priority = 5;
                        break;
                }
                switch (s["State"].ToString())
                {
                    case "Submitted":
                        def.p_state = 0;
                        break;
                    case "Open":
                        def.p_state = 1;
                        break;
                    case "Fixed":
                        def.p_state = 2;
                        break;
                    case "Closed":
                        def.p_state = 3;
                        break;
                }
                if (s["SubmittedBy"] != null)
                {

                    string SubmittedByObjectID = s["SubmittedBy"]._ref.ToString();
                    logger.Info("Get SubmittedBy");
                    var SubmittedByRest = GetUser(SubmittedByObjectID);
                    var userId = await GetUserIDPPM(SubmittedByRest.Result.EmailAddress.ToString());
                    def.p_submittedby = userId.ToString();
                }
                if (s["Owner"] != null)
                {
                    string OwnerObjectID = s["Owner"]._ref.ToString();
                    logger.Info("Get Owner");
                    var OwnerRest = GetUser(OwnerObjectID);
                    var userId = await GetUserIDPPM(OwnerRest.Result.EmailAddress.ToString());
                    def.p_owner = userId.ToString();
                }

                jDefect = JsonConvert.SerializeObject(def);
                logger.Info("Json Defect: " + jDefect);
            }
            catch (Exception ex)
            {

                logger.Error("Error function Create Project PPM - " + ex.Message);
            }

            return jDefect;
        }
        internal static async Task<string> IsUpdateDefect(string IdProject, string name)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/custDefectss?filter=%28code+%3D+%27" + name + "%27%29&fields=code";
            String userRef = "0";
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (content._totalCount > 0)
                        userRef = content._results[0]._internalId.ToString();
                }
                else
                    logger.Error("Status: " + response.StatusCode.ToString() + " - " + response.RequestMessage.ToString() + " - " + response.ReasonPhrase);
            }
            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return userRef.ToString();
        }
        internal static async Task CreateDefectOnProject(string IdProject, string Json)
        {
            logger.Info("Create Defect On Project");
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/custDefectss";
                                                                                          
            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                string jsonString = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Create Defect On Project Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Create Defect On Project Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Info("Create Defect on: " + IdProject + " : " + response.StatusCode.ToString());
                }

            }
            catch (TaskCanceledException e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
        }
        internal static async Task UpdateDefectOnProject(string IdProject, string Json, string code)
        {
            logger.Info("Update Defect On Project");
            string path = string.Empty;
            HttpContent content;
            HttpResponseMessage response;

            try
            {
                path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/custDefectss/" + code;

                var method = new HttpMethod("PATCH");

                content = new StringContent(Json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(method, path)
                {
                    Content = content
                };

                response = new HttpResponseMessage();

                response = await client.SendAsync(request);
                string jsonString = response.Content.ReadAsStringAsync().Result;

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Update Defects - Status: " + response.StatusCode.ToString() + " , Please check the information sent: " + Json);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("UpdateDefectOnProject - Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Error("Update Defect: " + code + " : " + response.StatusCode.ToString());
                }
            }
            catch (HttpRequestException e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }



        }
        #endregion

        //internal static async Task FindStoriesAndTheirTasksAsync(string ObjectID, string ProjectId, string codeFeature)
        //{
        //    logger.Warn("Find Stories And Their Tasks - Project:" + ProjectId);
        //    int storyCount = 0;
        //    int taskCount = 0;


        //    logger.Info(ApplicationConfig.IterationBackDay);
        //    logger.Info(ApplicationConfig.IterationForwardDay);

        //    DateTime today = DateTime.Today;
        //    DateTime forward = today.AddDays(ApplicationConfig.IterationForwardDay);
        //    String forwardString = forward.ToString("yyyy-MM-dd");
        //    DateTime back = today.AddDays(ApplicationConfig.IterationBackDay);
        //    String backString = back.ToString("yyyy-MM-dd");

        //    String projectRef = "/project/" + ObjectID;

        //    logger.Info("Reading Stories of Project: " + projectRef);


        //    Request request = new Request("HierarchicalRequirement");
        //    request.Workspace = workspaceRef;
        //    request.Fetch = new List<string>()
        //        {
        //            "Name",
        //            "ObjectID",
        //            "ScheduleState",
        //            "State",
        //            "FormattedID",
        //            "CreationDate",
        //            "ReleaseDate",
        //            "PlanEstimate",
        //            "Iteration",
        //            "StartDate",
        //            "EndDate",
        //            "Release",
        //            "ScheduleState",
        //            "Tasks",
        //        };
        //    request.Workspace = workspaceRef;
        //    request.Project = projectRef;


        //    request.Query = new Query("Iteration.StartDate", Query.Operator.GreaterThan, backString)
        //        .And(new Query("Iteration.EndDate", Query.Operator.LessThanOrEqualTo, forwardString));

        //    QueryResult queryResults = restApi.Query(request);

        //    logger.Warn("Criteria: " + request.Query.QueryClause.ToString());
        //    logger.Warn("Stories: " + queryResults.TotalResultCount);

        //    foreach (var s in queryResults.Results)
        //    {
        //        logger.Warn("Stories name: " + s["Name"]);


        //        string CodeUserStories = s["FormattedID"].ToString();
        //        var codeUserStoriesKeyTask = await IsUpdateUserStories(ProjectId, CodeUserStories);
        //        var JsonReturn = string.Empty;
        //        if (codeUserStoriesKeyTask.ToString() != "0")
        //        {
        //            string codeUserStories = codeUserStoriesKeyTask.ToString();
        //            JsonReturn = CreateUserStoriesKeyTask(s, ProjectId, true, ProjectId);
        //            await UpdateUserStories(ProjectId, codeUserStories, JsonReturn);
        //        }
        //        else
        //        {
        //            JsonReturn = CreateUserStoriesKeyTask(s, ProjectId, false, ProjectId);
        //            codeUserStoriesKeyTask = await CreateUserStories(ProjectId, JsonReturn);
        //        }

        //        Request tasksRequest = new Request(s["Tasks"]);

        //        QueryResult queryTaskResult = restApi.Query(tasksRequest);
        //        logger.Warn("Tasks: " + queryTaskResult.TotalResultCount);
        //        foreach (var t in queryTaskResult.Results)
        //        {
        //            logger.Warn("Task Name: " + t["Name"]);
        //            string codeTaskRall = t["FormattedID"].ToString();
        //            var codeKeyTask = await IsUpdateUserStories(ProjectId, codeTaskRall);

        //            var JsonReturnTask = string.Empty;
        //            string codeTask = codeKeyTask;
        //            if (codeKeyTask.ToString() != "0")
        //            {
        //                string codeUserStories = codeKeyTask.ToString();
        //                JsonReturnTask = await CreateTasksAsync(t, codeUserStoriesKeyTask, s, ProjectId, true);

        //                await UpdateTasksPPM(ProjectId, codeTask, JsonReturnTask);

        //            }
        //            else
        //            {
        //                JsonReturnTask = await CreateTasksAsync(t, codeUserStoriesKeyTask, s, ProjectId, false);
        //                codeTask = await CreateTasksPPM(ProjectId, JsonReturnTask);
        //            }

        //           // var JsonReturnAssing = await AssingTasksPPMAsync(t, c);
        //            await AssingTaskToUserAsync(ProjectId, codeTask, JsonReturnAssing);
        //            taskCount++;
        //        }
        //    }

        //}

        private static async Task<string> AssingTaskToUserAsync(string projectId, string codeTask, string Json)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + projectId + "/tasks/" + codeTask + "/assignments";
            string _internalId = "0";
            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                string jsonString = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    dynamic respcontent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    _internalId = respcontent._internalId.ToString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Task has already been assignment to the resource - Project:" + projectId + " Task:" + codeTask);// + " ErrorMessage:" + respcontent.errorMessage);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("AssingTaskToUser: " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Warn("AssingTaskToUser: " + _internalId + " : " + response.StatusCode.ToString());
                }

            }
            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return _internalId.ToString();
        }

        private static async Task<string> AssingTasksPPMAsync(string _internalId, string startDate, string finishDate)
        {
            logger.Info("Assing Tasks PPM");
            string jAssing = string.Empty;
            Assignment Assing;
            try
            {
                Assing = new Assignment();
                DateTime dStartDate = DateTime.ParseExact(startDate, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                DateTime dfinishDate = DateTime.ParseExact(finishDate, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                Assing.startDate = dStartDate.ToString("yyyy-MM-ddTHH:mm:ss");//, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString(); //);
                Assing.finishDate = dfinishDate.ToString("yyyy-MM-ddTHH:mm:ss");


                if (_internalId != null)
                {
                    //string Owner = t["Owner"]._ref.ToString();
                    //logger.Info("Get Assing Task");
                    //var OwnerRest = GetUser(Owner); 
                    //var _internalId = await GetUserIDPPM("r2admin@fmh.com");
                    ////var _internalId = await GetUserIDPPM(OwnerRest.Result.EmailAddress);
                    Assing.resource = _internalId;
                }


                jAssing = JsonConvert.SerializeObject(Assing);

            }
            catch (Exception ex)
            {

                logger.Error("Error function Create Json Assing Tasks PPM - " + ex.Message);
            }

            return jAssing;
        }

        private static async Task<string> UpdateUserStories(string projectId, string codeUserStoriesKeyTask, string json)
        {
            logger.Info("UpdateUserStories");
            string path = string.Empty;
            string _internalId = "0";
            HttpContent content;
            HttpResponseMessage response;

            try
            {
                path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + projectId + "/tasks/" + codeUserStoriesKeyTask;

                var method = new HttpMethod("PATCH");

                content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(method, path)
                {
                    Content = content
                };

                response = new HttpResponseMessage();

                response = await client.SendAsync(request);
                dynamic respcontent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                //string jsonString = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    _internalId = respcontent._internalId.ToString();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("UpdateUserStories - Status: " + response.StatusCode.ToString() + " ErrorMessage:" + respcontent.errorMessage);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("UpdateUserStories - Status: " + response.StatusCode.ToString() + "Json" + json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Info("UpdateUserStories: " + codeUserStoriesKeyTask + " : " + response.StatusCode.ToString());
                }
            }
            catch (HttpRequestException e)
            {
                logger.Error("ERROR - UpdateUserStories: " + e.ToString());
            }
            catch (Exception e)
            {
                logger.Error("ERROR - UpdateUserStories: " + e.ToString());
            }
            return _internalId.ToString();
        }

        private static async Task<string> CreateUserStories(string IdProject, string Json)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/tasks";
            string _internalId = "0";
            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                string jsonString = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    dynamic respcontent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    _internalId = respcontent._internalId.ToString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Create User Stories: " + response.StatusCode.ToString() + "Json" + Json);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Create User Stories: " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Warn("Create User Stories: " + _internalId + " : " + response.StatusCode.ToString());
                }

            }
            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return _internalId.ToString();
        }

        private static async Task<string> CreateTasksPPM(string IdProject, string Json)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/tasks";
            string _internalId = "0";
            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                string jsonString = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    dynamic respcontent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    _internalId = respcontent._internalId.ToString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Create Tasks PPM Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Create Tasks PPM Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Error("Create Tasks PPM on: " + IdProject + " : " + response.StatusCode.ToString());
                }

            }
            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return _internalId.ToString();
        }

        private static async Task<string> UpdateTasksPPM(string projectId, string codeKeyTask, string json)
        {
            logger.Info("Update Tasks PPM");
            string path = string.Empty;
            string _internalId = "0";
            HttpContent content;
            HttpResponseMessage response;

            try
            {
                path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + projectId + "/tasks/" + codeKeyTask;

                var method = new HttpMethod("PATCH");

                content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(method, path)
                {
                    Content = content
                };

                response = new HttpResponseMessage();

                response = await client.SendAsync(request);
                //string jsonString = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    dynamic respcontent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    _internalId = respcontent._internalId.ToString();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Update Tasks PPM - Status: " + response.StatusCode.ToString() + "Json" + json);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Update Tasks PPM - Status: " + response.StatusCode.ToString() + "Json" + json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Warn("Update Tasks PPM: " + codeKeyTask + " : " + response.StatusCode.ToString());
                }
            }
            catch (HttpRequestException e)
            {
                logger.Error("ERROR - Update Tasks PPM: " + e.ToString());
            }
            catch (Exception e)
            {
                logger.Error("ERROR - Update Tasks PPM: " + e.ToString());
            }
            return _internalId.ToString();
        }

        private static async Task<string> IsUpdateUserStories(string IdProject, string CodeUserStories)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/tasks?filter=%28code+%3D+%27" + CodeUserStories + "%27%29&fields=code";
            String _internalId = "0";
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (content._totalCount > 0)
                        _internalId = content._results[0]._internalId.ToString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Get ID User Stories PPM:: " + IdProject + " - Status: " + response.StatusCode.ToString() + "Json" + CodeUserStories);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Error("Get ID User Stories PPM: " + IdProject + " : " + response.StatusCode.ToString());
                }
            }

            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return _internalId.ToString();
        }

        private static async Task<string> CreateTasksAsync(dynamic t, string parentTask, dynamic s, string ProjectId, bool isUpdate)
        {
            logger.Info("Create Task");
            string jTask = string.Empty;
            Tasks task;
            try
            {
                task = new Tasks();
                task.code = t["FormattedID"].ToString();

                task.name = StringExt.StripHTML(t["Name"].ToString());
                DateTime StartDate = DateTime.Now;
                DateTime finishDate = DateTime.Now;

                if (!s["Iteration"])
                {
                    StartDate = DateTime.Parse(s["Iteration"].StartDate);
                    finishDate = DateTime.Parse(s["Iteration"].EndDate);
                }



                task.startDate = StartDate.ToString("yyyy-MM-ddTHH:mm:ss");//, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString(); //);
                task.finishDate = finishDate.ToString("yyyy-MM-ddTHH:mm:ss");

                switch (t["State"].ToString())
                {
                    case "Defined":
                        task.status = 0;
                        break;
                    case "In-Progress":
                        task.status = 1;
                        break;
                    case "Completed":
                        task.status = 2;
                        break;
                    default:
                        task.status = 0;
                        break;
                }

                task.priority = 0;
                task.isTask = true;
                task.isKey = false;
                task.isMilestone = false;
                task.parentTask = parentTask;
                task.isOpenForTimeEntry = true;
                //task.agileExternalID = t["ObjectID"].ToString();
                string EmailAddress = "admin";
                if (t["Owner"] != null)
                {
                    string Owner = t["Owner"]._ref.ToString();
                    logger.Info("Get Assing Task");
                    var OwnerRest = GetUser(Owner);
                    EmailAddress = OwnerRest.Result.EmailAddress;
                    //task.taskOwner = OwnerRest.Result.EmailAddress;
                }

                //After get Tasks owner create a team
                var _internal = await GetUserIDPPM(EmailAddress);
                var r = await CreateTeamPPM(ProjectId, _internal.ToString());

                jTask = JsonConvert.SerializeObject(task);

                if (isUpdate)
                {
                    JObject jo = JObject.Parse(jTask);
                    jo.Property("code").Remove();
                    jTask = jo.ToString();

                }
                logger.Info("Json CreateUserStoriesKeyTask: " + jTask);

            }
            catch (Exception ex)
            {

                logger.Error("Error function CreateUserStoriesKeyTask - " + ex.Message);
            }

            return jTask;
        }

        private static int GetReturnKeytask(dynamic response)
        {
            int _internalId = 0;

            dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            if (content._totalCount > 0)
                _internalId = content._results[0]._internalId.ToString();
            return _internalId;
        }



        private static async Task<string> CreateUserStoriesKeyTask(dynamic p, string parentTask, bool isUpdate, dynamic t)
        {
            logger.Info("CreateUserStoriesKeyTask");
            string jTask = string.Empty;
            Tasks task;
            try
            {
                task = new Tasks();
                task.code = p["FormattedID"].ToString();
                task.name = StringExt.Truncate(StringExt.StripHTML(p["Name"].ToString()), 150);
                DateTime StartDate;
                DateTime finishDate;

                if (p["Iteration"] == null)
                {
                    if (p["PlannedStartDate"] == null)
                    {
                        DateTime adate = DateTime.Parse(t["CreationDate"]);
                        StartDate = adate;
                        finishDate = adate;

                    }
                    else
                    {
                        StartDate = DateTime.Parse(p["PlannedStartDate"]);
                        finishDate = DateTime.Parse(p["PlannedEndDate"]);
                    }


                }
                else
                {
                    if (p["Iteration"] == null)
                    {
                        StartDate = DateTime.Now;
                        finishDate = DateTime.Now;

                    }
                    else
                    {
                        StartDate = DateTime.Parse(p["Iteration"].StartDate);
                        finishDate = DateTime.Parse(p["Iteration"].EndDate);
                    }

                }


                task.startDate = StartDate.ToString("yyyy-MM-ddTHH:mm:ss");//, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString(); //);
                task.finishDate = finishDate.ToString("yyyy-MM-ddTHH:mm:ss");
                task.status = 1;
                task.priority = 10;
                task.isTask = true;
                task.isKey = false;
                task.isMilestone = false;
                task.isOpenForTimeEntry = true;

                if (parentTask != "0")
                    task.parentTask = parentTask;
                else
                    task.parentTask = null;





                // task.agileExternalID = p["ObjectID"].ToString();

                jTask = JsonConvert.SerializeObject(task);
                if (isUpdate)
                {
                    JObject jo = JObject.Parse(jTask);
                    jo.Property("code").Remove();
                    jo.Property("isTask").Remove();

                    jTask = jo.ToString();

                }
                //logger.Info("Json CreateUserStoriesKeyTask: " + jTask);

            }
            catch (Exception ex)
            {

                logger.Error("Error function CreateUserStoriesKeyTask - " + ex.Message);
            }

            return jTask;


        }

        internal static Task<User> GetUser(string UserId)
        {
            logger.Info("User ID: " + UserId);
            if (UserId.IndexOf("/") > 0)
            {
                string UserIdReverse = new string(UserId.Reverse().ToArray());
                UserId = UserIdReverse.Substring(0, UserIdReverse.IndexOf("/"));
                UserId = new string(UserId.Reverse().ToArray());
            }



            String workspaceRef = "/workspace/" + ApplicationConfig.RallyWorkspace;
            Request userRequest = new Request("User");
            userRequest.Workspace = workspaceRef;

            userRequest.Query = new Query("(ObjectID = " + UserId + ")");

            QueryResult userResults;
            User resource = new User();
            try
            {
                userResults = restApi.Query(userRequest);
                String userRef = userResults.Results.First()._ref;
                DynamicJsonObject user = restApi.GetByReference(userRef, "Name", "Role", "DisplayName", "EmailAddress", "FirstName",
                    "LastName", "UserName");

                resource.Role = user["Role"];
                resource.DisplayName = user["DisplayName"];
                resource.EmailAddress = user["EmailAddress"];
                resource.FirstName = user["FirstName"];
                resource.LastName = user["LastName"];
                resource.UserName = user["UserName"];

                logger.Info("DisplayName: " + resource.DisplayName);
                logger.Info("EmailAddress: " + resource.EmailAddress);

            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (Exception)
            {
                logger.Error("User not found:" + UserId);
                resource.EmailAddress = ApplicationConfig.PPMResourceDefault;
            }

            ////Retrar isso 
            //resource.EmailAddress = "joshd@fmh.com";

            return Task.FromResult(resource);

        }
        private static async Task<string> GetUserIDPPM(string EmailUser)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/resources?filter=(email%20%3D%20'" + EmailUser + "')";
            String _internalId = "0";
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (content._totalCount > 0)
                    {
                        _internalId = content._results[0]._internalId.ToString();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("GetUserIDPPM - Status: " + response.StatusCode.ToString());
                }

            }

            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            if (_internalId.ToString() == "0")
                _internalId = "1";
            return _internalId.ToString();
        }
        private static async Task<string> CreateTeamPPM(string IdProject, string _internalId)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/projects/" + IdProject + "/teams";

            string Json = CreateTeam(_internalId);

            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                string jsonString = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    dynamic respcontent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (respcontent._totalCount > 0)
                        _internalId = respcontent._results[0]._internalId.ToString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("User has already been allocated to the Project");// + " ErrorMessage:" + respcontent.errorMessage);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Create Team PPM User : " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Warn("User allocated successfully to the Project");
                }

            }
            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return _internalId.ToString();
        }
        private static string CreateTeam(string internalId)
        {
            logger.Info("CreateTeam");
            string jTeam = string.Empty;
            Team team;
            try
            {
                team = new Team();
                team.resource = internalId;
                team.isOpenForTimeEntry = true;
                team.isActive = true;
                team.isRole = false;

                jTeam = JsonConvert.SerializeObject(team);
                logger.Info("Json Projet: " + jTeam);

            }
            catch (Exception ex)
            {

                logger.Error("Error function CreateProjectPPM - " + ex.Message);
            }

            return jTeam;
        }
    }
}
