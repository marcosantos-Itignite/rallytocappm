using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ItigniteRallyIntegration.Model;
using Newtonsoft.Json;

namespace ItigniteRallyIntegration
{
    public class PPMRestApi
    {
       

        /// <summary>Post call to get access key from SDM.</summary>
        /// <param name="sdmURL">URL for server (Format: http://hostname:port/caisd-rest</param>
        /// <param name="username">Username of the SDM user.</param>
        /// <param name="password">Password of the SDM user.</param>
        /// <returns>Access Key to be passed as X-AccessKey header value in subsequent calls </returns>
        /// 
        public  string Authenticate(string sdmRESTURL, string username, string password)
        {
            string postBody = @"<rest_access/>";
            byte[] dataByte = Encoding.ASCII.GetBytes(postBody);

            HttpWebRequest POSTRequest = (HttpWebRequest)WebRequest.Create(sdmRESTURL + @"/rest_access");
            //Method type
            POSTRequest.Method = "POST";
            // Data type - message body coming in xml
            POSTRequest.ContentType = "application/xml";
            POSTRequest.Accept = "application/json";
            POSTRequest.KeepAlive = false;
            POSTRequest.Timeout = 5000;

            //Encodes Username and Password to Base64
            string credentials = String.Format("{0}:{1}", username, password);
            byte[] bytes = Encoding.ASCII.GetBytes(credentials);
            string base64 = Convert.ToBase64String(bytes);
            string authorization = String.Concat("Basic ", base64);
            POSTRequest.Headers.Add(HttpRequestHeader.Authorization, authorization);

            //Content length of message body
            POSTRequest.ContentLength = dataByte.Length;

            // Get the request stream
            Stream POSTstream = POSTRequest.GetRequestStream();
            // Write the data bytes in the request stream
            POSTstream.Write(dataByte, 0, dataByte.Length);

            //Get response from server
            try
            {
                HttpWebResponse POSTResponse = (HttpWebResponse)POSTRequest.GetResponse();
                StreamReader reader = new StreamReader(POSTResponse.GetResponseStream(), Encoding.UTF8);

                //responseString is the full responseString
                string responseString = reader.ReadToEnd().ToString();

                dynamic authResponse = JsonConvert.DeserializeObject(responseString);

                //accessKey is to be passed as X-AccessKey header value in subsequent calls
                string accessKey = authResponse.rest_access.access_key;

                return accessKey;
            }
            //Catch HTTP Error Codes
            catch (WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                return "HTTP Error " + (int)response.StatusCode + ": " + response.StatusCode;
            }
            //Catch All Other Errors
            catch
            {
                return "Unexpected Error";
            }
        }



        public static void AuthenticatePPM(string UserName, string Password, string Url)
        {
            

        }

        }
}
