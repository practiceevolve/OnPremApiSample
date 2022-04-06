using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Linq;

namespace GetMattersForFeeEarnerExample
{
    internal class Program
    {
        /// <summary>
        /// Put your api endpoint here
        /// </summary>
        internal static string ApiUrl = "";
        
        /// <summary>
        /// Put your client key here (see documentation on how to generate this)
        /// </summary>
        internal static string ClientKey = "";

        /// <summary>
        /// Put your user key here (see documentation on how to generate this)
        /// </summary>
        internal static string UserKey = "";
        
        internal static HttpClient client;
        
        static void Main(string[] args) {

            if (args.Length == 0) {
                Console.WriteLine("Hey I need a username query string");
                return;
            }
            var username = args[0];

            // Building out a basic HttpClient, giving the base url as the Uri and adding the keys as default headers.
            // They will be required on all calls so this is just easier.

            client = new HttpClient();
            client.BaseAddress = new Uri(ApiUrl);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Client-Key", ClientKey);
            client.DefaultRequestHeaders.Add("User-Key", UserKey);

            // Getting employee name for later
            var employeeId = GetEmployeeName(username);

            // Getting an array of all that employees matters
            var matters = GetMatters(username);

            // Printing the result
            Console.WriteLine($"Fee earner {employeeId}'s matters");
            foreach (var matter in matters) {
                Console.WriteLine(matter);
            }
        }

        private static string GetEmployeeName(string username) {
            // Calling the get user by username endpoint with just one arg.
            var result = client.GetAsync("/api/v1/employees?userName=" + username).Result;

            // This is a simple method that makes sure that the result we got was a success and turns it into a json
            var json = ValidateAndGetJson(result);

            // This method extracts the employee freindly name for use later
            var obj = GetEmployeeObjectsFromResult(json);
            return obj.Count > 0 ? obj.Values.FirstOrDefault().Name : null;
        }

        private static string[] GetMatters(string username) {
            var result = client.GetAsync("api/v1/matters?feeEarner=" + username).Result;

            var json = ValidateAndGetJson(result);

            return GetMatterObjectsFromResult(json).Values.Select(m => m.Name).ToArray();
        }

        private static JsonDocument ValidateAndGetJson(HttpResponseMessage result) {

            // The HTTP request needs to have succeeded, if not you've got a bad uri
            if (result.IsSuccessStatusCode) {

                var json = JsonDocument.Parse(result.Content.ReadAsStream());

                // Our return objects return a success parameter which lets you know if the query was truly successful or if there was an error in the business logic
                if (json.RootElement.GetProperty("success").ValueKind == JsonValueKind.True) {
                    return json;
                }
                else
                    Console.WriteLine("Bad request: " + json.RootElement.GetProperty("error").GetProperty("displayMessage").ToString());
            }
            else {
                Console.WriteLine("Bad request: " + result.ReasonPhrase);
            }
            return null;
        }

        private static Dictionary<string, SimpleObject> GetEmployeeObjectsFromResult(JsonDocument json) {
            var returnDic = new Dictionary<string, SimpleObject>();
            foreach (var item in json.RootElement.GetProperty("employees").GetProperty("rows").EnumerateArray()) {
                var employee = new SimpleObject();
                employee.ID = item.GetProperty("employeeId").GetString();
                employee.Name = item.GetProperty("preferredName").GetString();
                returnDic.Add(employee.ID, employee);
            }
            return returnDic;
        }


        private static Dictionary<string, SimpleObject> GetMatterObjectsFromResult(JsonDocument json) {
            var returnDic = new Dictionary<string, SimpleObject>();
            foreach (var item in json.RootElement.GetProperty("matters").GetProperty("rows").EnumerateArray()) {
                var matter = new SimpleObject();
                matter.ID = item.GetProperty("id").GetString();
                matter.Name = item.GetProperty("matterNumber").GetString() + ": " + item.GetProperty("title").GetString();
                returnDic.Add(matter.ID, matter);
            }
            return returnDic;
        }
    }

    internal class SimpleObject
    {
        public string Name { get; set; }
        public string ID { get; set; }
    }
}
