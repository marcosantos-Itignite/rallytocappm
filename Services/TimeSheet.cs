using Integration;
using ItigniteRallyIntegration.Core;
using ItigniteRallyIntegration.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Services
{
    public class TimeSheet
    {
        static HttpClient client;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public TimeSheet(HttpClient Client)
        {
            client = Client;
        }

        //GET /timePeriods filter by (((startDate >= '2019-11-25T00:00:00')  and (finishDate <= '2019-12-02T00:00:00')))
        //http://onetimesheet.com:5081/ppm/rest/v1/timePeriods?fields=startDate%2CfinishDate&filter=((startDate%20%3E%3D%20'2019-11-25T00%3A00%3A00')%20%20and%20(finishDate%20%3C%3D%20'2019-12-02T00%3A00%3A00'))
        public async Task<int>  GetTimePeriods(DateTime startDate, DateTime finishDate)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/timePeriods?fields=startDate%2CfinishDate&filter=((startDate%20%3E%3D%20'"+startDate.ToString("yyyy-MM-dd") + "T00%3A00%3A00')%20%20and%20(finishDate%20%3C%3D%20'" + finishDate.ToString("yyyy-MM-dd") + "T00%3A00%3A00'))";
            int timePeriodId = 0;
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (content._totalCount > 0)
                    {
                        timePeriodId = content._results[0]._internalId;
                        startDate = DateTime.Parse(content._results[0].startDate.ToString());
                        finishDate = DateTime.Parse(content._results[0].finishDate.ToString());
                    }
                    else
                    {
                        throw new Exception("Period start:" + startDate.ToString() + " Finish:" + finishDate.ToString()  + " Not Found");
                    }
                        
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Get Time Periods - Status: " + response.StatusCode.ToString());
                }
                
            }

            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            return timePeriodId;
            
        }

        //GET /timesheets  (timesheetsInternalId and timeEntriesInternalId) filter by (timePeriodId = 5001039) and (timeEntries=(filter=((taskId = 5008246))))
        //http://onetimesheet.com:5081/ /timesheets?fields=timePeriodStart%2CtimePeriodFinish&filter=(timePeriodId%20%3D%205001039)&expand=(timeEntries%3D(filter%3D((taskId%20%3D%205008246))))
        public async Task<HelperTimeSheetId> GetTimeSheets(int timePeriodId, int taskId, string resourceID)
        {
            string path = ApplicationConfig.PPMUrl + "/rest/v1/timesheets?fields=resourceId%2CtimePeriodStart%2CtimePeriodFinish&filter=((timePeriodId%20%3D%20" + timePeriodId + ")%20and%20(resourceId%20%3D%20" + resourceID + "))&expand=(timeEntries%3D(filter%3D((taskId%20%3D%20" + taskId+"))))";
            HelperTimeSheetId helperTimeSheetIds = new HelperTimeSheetId();
           
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (content._totalCount > 0)
                    {

                        helperTimeSheetIds._timesheetsInternalId = content._results[0]._internalId;
                        if (content._results[0].timeEntries._totalCount > 0)
                            helperTimeSheetIds._timeEntriesInternalId = content._results[0].timeEntries._results[0]._internalId;
                    }
                    else
                    {
                        logger.Error("Get TimeSheets:" + timePeriodId + " taskId:" + taskId + " Not Found/Owner not found Or not Open to entry timesheet ");
                    }

                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Get TimeSheets - Status: " + response.StatusCode.ToString());
                }

            }

            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            //timesheetsInternalId and timeEntriesInternalId
            return helperTimeSheetIds;
        }

        //POST /timesheets
        //http://onetimesheet.com:5081/ppm/rest/v1/timesheets
        public async Task<int> PostTimeSheet(int timePeriodId, string resourceId)
        {
            logger.Info("Post TimeSheet");
            int _timesheetsInternalId = 0;
            string path = ApplicationConfig.PPMUrl + "/rest/v1/timesheets/";

            string Json = @"{ ""timePeriodId"" : " + timePeriodId + ", \"resourceId\" : "+ resourceId + ", \"status\": \"0\" }";
            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                if (response.IsSuccessStatusCode)
                {
                    dynamic resultContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                        _timesheetsInternalId = resultContent._internalId;
                }


                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Post TimeSheet - Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Post TimeSheet - Status: " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Error("Post TimeSheet: " + response.StatusCode.ToString());
                }
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (TaskCanceledException e)
            {
                logger.Error("ERROR: " + e.ToString());
            }

            //{
            //  "timePeriodId": 5001040,
            //  "resourceId": 1,
            //  "status": "0"
            //}

            //timesheetsInternalId :"_internalId": 5001004,
            return _timesheetsInternalId;
        }
        
        //POST /timesheets/{timesheetsInternalId}/timeEntries
        //http://onetimesheet.com:5081/ppm/rest/v1/timesheets/5001004/timeEntries
        public async Task<int> PostTimeSheetEntries(int timesheetsInternalId, int taskId)
        {
            logger.Info("Post TimeSheet Entries");
            int _timeEntriesInternalId = 0;
            string path = ApplicationConfig.PPMUrl + "/rest/v1/timesheets/"+timesheetsInternalId+"/timeEntries";


            string Json = @"{ ""taskId"" : "+ taskId+" }";


            HttpContent content = new StringContent(Json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                response = await client.PostAsync(path, content);
                
                if (response.IsSuccessStatusCode)
                {
                    dynamic resultContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    resultContent = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    _timeEntriesInternalId = resultContent._internalId;
                   
                }
                

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("Post TimeSheet Entries - Status: " + response.StatusCode.ToString() + " TimeSheet not avaible for add task " + taskId);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Client not available " + response.StatusCode.ToString() + "Json" + Json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Error("Post TimeSheet Entries: " + response.StatusCode.ToString());
                }
            }
            catch (HttpRequestException e)
            {
                logger.Error("Connection error with client PPM: " + e.InnerException.Message);
            }
            catch (TaskCanceledException e)
            {
                logger.Error("ERROR: " + e.ToString());
            }
            //{
            //    "taskId": 5008245
            //}

            //"timeEntriesInternalId _internalId": 5001142,
            return _timeEntriesInternalId;
        }

       // PATCH /timesheets/{timesheetsInternalId}/timeEntries/{timeEntriesInternalId
       public async Task<int> PatchTimeSheetEntries(HelperTimeSheetId timesheetsInternalIds, int TaskId, TasksRally tasksRally, List<segments> ListSegments)
       {
            logger.Info("Patch TimeSheetEntries");
            string path = string.Empty;
            int _internalId = 0;
            HttpContent content;
            HttpResponseMessage response;

            try
            {
                path = ApplicationConfig.PPMUrl + "/rest/v1/timesheets/"+timesheetsInternalIds._timesheetsInternalId+"/timeEntries/"+ timesheetsInternalIds._timeEntriesInternalId;

                var method = new HttpMethod("PATCH");
                string json = createTimeSheetEntries(tasksRally, TaskId, ListSegments);
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
                    _internalId = respcontent._internalId;
                    logger.Warn("Timesheet Update:" + _internalId.ToString());
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.Error("PatchTimeSheetEntries - Status: " + response.StatusCode.ToString() + " ErrorMessage:" + respcontent.errorMessage);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("PatchTimeSheetEntries - Status: " + response.StatusCode.ToString() + "Json" + json);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Info("Patch TimeSheet Entries: " + TaskId + " : " + response.StatusCode.ToString());
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
            //            {
            //                "taskId": 5008246,
            //  "actuals": {
            //                    "segmentList": {
            //                        "segments": [
            //                          {
            //          "start": "2019-12-25T00:00:00",
            //                            "finish": "2019-11-25T00:00:00",
            //                            "value": 7200
            //        },
            //        {
            //          "start": "2019-11-26T00:00:00",
            //          "finish": "2019-11-26T00:00:00",
            //          "value": 18000
            //        },
            //        {
            //          "start": "2019-11-27T00:00:00",
            //          "finish": "2019-11-27T00:00:00",
            //          "value": 7200
            //        },
            //        {
            //          "start": "2019-11-28T00:00:00",
            //          "finish": "2019-11-28T00:00:00",
            //          "value": 18000
            //        },
            //        {
            //          "start": "2019-11-29T00:00:00",
            //          "finish": "2019-11-29T00:00:00",
            //          "value": 0
            //        },
            //        {
            //          "start": "2019-11-30T00:00:00",
            //          "finish": "2019-11-30T00:00:00",
            //          "value": 0
            //        },
            //        {
            //          "start": "2019-12-01T00:00:00",
            //          "finish": "2019-12-01T00:00:00",
            //          "value": 0
            //        }
            //      ]
            //    }
            //  }
            //}
            return _internalId;
       }

        //GET / http://onetimesheet.com:5081/ppm/rest/v1/timesheets/5001001/timeEntries/5001110
        public async Task<List<segments>> SegmentsTimeSheetEntries(HelperTimeSheetId timesheetsInternalIds)
        {
            logger.Info("Segments TimeSheet Entries");
            string path = ApplicationConfig.PPMUrl + "/rest/v1/timesheets/" + timesheetsInternalIds._timesheetsInternalId + "/timeEntries/" + timesheetsInternalIds._timeEntriesInternalId;

            HttpResponseMessage response = new HttpResponseMessage();
            dynamic segmentsList = null;
            List<segments> items = null;
            try
            {
                response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    dynamic content = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    segmentsList = content.actuals.segmentList.segments;

                    items = ((JArray)segmentsList).Select(x => new segments
                    {
                        start = DateTime.Parse(x["start"].ToString()),
                        finish = DateTime.Parse(x["finish"].ToString()),
                        value = Decimal.Parse(x["value"].ToString())
                    }).ToList();


                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.Error("Segments TimeSheet Entries - Status: " + response.StatusCode.ToString());
                }

            }

            catch (Exception e)
            {
                logger.Error("ERROR: " + e.ToString());
            }

           //            {
            //                "taskId": 5008246,
            //  "actuals": {
            //                    "segmentList": {
            //                        "segments": [
            //                          {
            //          "start": "2019-12-25T00:00:00",
            //                            "finish": "2019-11-25T00:00:00",
            //                            "value": 7200
            //        },
            //        {
            //          "start": "2019-11-26T00:00:00",
            //          "finish": "2019-11-26T00:00:00",
            //          "value": 18000
            //        },
            //        {
            //          "start": "2019-11-27T00:00:00",
            //          "finish": "2019-11-27T00:00:00",
            //          "value": 7200
            //        },
            //        {
            //          "start": "2019-11-28T00:00:00",
            //          "finish": "2019-11-28T00:00:00",
            //          "value": 18000
            //        },
            //        {
            //          "start": "2019-11-29T00:00:00",
            //          "finish": "2019-11-29T00:00:00",
            //          "value": 0
            //        },
            //        {
            //          "start": "2019-11-30T00:00:00",
            //          "finish": "2019-11-30T00:00:00",
            //          "value": 0
            //        },
            //        {
            //          "start": "2019-12-01T00:00:00",
            //          "finish": "2019-12-01T00:00:00",
            //          "value": 0
            //        }
            //      ]
            //    }
            //  }
            //}
            return items;
        }
        
        private string createTimeSheetEntries(TasksRally tasksRally, int TaskId, List<segments> ListSegments)
        {
            ItigniteRallyIntegration.Model.TimeSheet timeSheet = new Model.TimeSheet();
            // Get Interval Week Date task
            HelpDate.GetIntervalWeek(tasksRally.entriesDate, out DateTime startDate, out DateTime finishDate);

            timeSheet.taskId = TaskId;
           
            ListSegments.Where(q => q.start.ToString("yyyy-mm-dd") == tasksRally.entriesDate.ToString("yyyy-mm-dd"))
                .ToList().ForEach(b => b.value = (tasksRally.value * 3600));

            timeSheet.actuals.segmentList.segments.AddRange(ListSegments);
            foreach (var item in ListSegments)
            {
                logger.Warn("Start: " + item.start + " Finish: " + item.finish + " Hours:" + item.value);
            }
            

            //for (int i = 0; i < 7; i++)
            //{
            //    logger.Info(ListSegments.startDate);
            //    if (tasksRally.entriesDate == ListSegments.startDate)
            //    {
            //        timeSheet.actuals.segmentList.segments.Add(new segments
            //        {
            //            start = startDate.AddDays(i),
            //            finish = startDate.AddDays(i),
            //            value = (tasksRally.value * 3600)
            //        });
            //    }
            //    else
            //    {
            //        timeSheet.actuals.segmentList.segments.Add(new segments
            //        {
            //            start = startDate.AddDays(i),
            //            finish = startDate.AddDays(i),
            //            value = 0
            //        });
            //    }

            //}


            return JsonConvert.SerializeObject(timeSheet);
        }
    }
}
