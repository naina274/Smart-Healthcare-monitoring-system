using Healthcarefunctions = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Microsoft.Azure.WebJobs.Extensions.CosmosDB;
using System;
using System.Collections;

namespace IotData.Function
{
    public class SensorData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public double TemperatureAlert { get; set; }
        public double BloodPressureAlert { get; set; }
        public double HeartRateAlert { get; set; }
    }

    public static class IoTData
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("Healthcarefunctions")]
        public static void Run([Healthcarefunctions("messages/events", Connection = "AzureEventHubConnectionString")] EventData message,
                               [CosmosDB(databaseName: "IoTData",
                                         collectionName: "sensordata",
                                         ConnectionStringSetting = "cosmosDBConnectionString")] out SensorData output,
                               ILogger log)
        {
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");

            var jsonBody = Encoding.UTF8.GetString(message.Body);
            dynamic data = JsonConvert.DeserializeObject(jsonBody);
            double? temperature = data.temperature;
            double? bloodpressure = data.bloodpressure;
            double? heartrate = data.heartrate;

            output = new SensorData();

            if (temperature.HasValue)
            {
                output.TemperatureAlert = (double)temperature.Value;
            }

            if (bloodpressure.HasValue)
            {
                output.BloodPressureAlert = (double)bloodpressure.Value;
            }

            if (heartrate.HasValue)
            {
                output.HeartRateAlert = (double)heartrate.Value;
            }

            output.Id = Guid.NewGuid().ToString();
        }
        [FunctionName("GetDatafunction")]
        public static IActionResult GetDataFunction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sensordata/")] HttpRequest req,
        [CosmosDB(databaseName: "IoTData",
              collectionName: "sensordata",
              ConnectionStringSetting = "cosmosDBConnectionString",
              SqlQuery = "SELECT * FROM c")] IEnumerable SensorData,
        ILogger log)
    {
        return new OkObjectResult(SensorData);
    }

    }
}
