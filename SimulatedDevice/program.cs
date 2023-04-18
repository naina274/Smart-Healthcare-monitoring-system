using Microsoft.Azure.Devices.Client;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHealthcareMonitor
{
    internal class Program
    {
        private static DeviceClient s_deviceClient;
        private static readonly TransportType s_transportType = TransportType.Mqtt;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string s_connectionString = "HostName=healthcarehub.azure-devices.net;DeviceId=healthcare_simdevice;SharedAccessKey=Yww6WBH4R4mtdIXqeLQ6zSc8D/d97cG/Hr3gfCwbRzU=";

        // Set the desired ranges for vital signs
        private static readonly double minTemperature = 36.5;
        private static readonly double maxTemperature = 37.5;
        private static readonly double minBloodPressure = 70;
        private static readonly double maxBloodPressure = 130;
        private static readonly double minHeartRate = 60;
        private static readonly double maxHeartRate = 100;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Smart Healthcare Monitor - Simulated device.");

            // This sample accepts the device connection string as a parameter, if present
            ValidateConnectionString(args);

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            // Set up a condition to quit the sample
            Console.WriteLine("Press control-C to exit.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Run the telemetry loop
            await SendDeviceToCloudMessagesAsync(cts.Token);

            s_deviceClient.Dispose();
            Console.WriteLine("Device simulator finished.");
        }

        private static void ValidateConnectionString(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    var cs = IotHubConnectionStringBuilder.Create(args[0]);
                    s_connectionString = cs.ToString();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error: Unrecognizable parameter '{args[0]}' as connection string.");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    _ = IotHubConnectionStringBuilder.Create(s_connectionString);
                }
                catch (Exception)
                {
                    Console.WriteLine("This sample needs a device connection string to run. Program.cs can be edited to specify it, or it can be included on the command-line as the only parameter.");
                    Environment.Exit(1);
                }
            }
        }

        // Async method to send simulated telemetry
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            var rand = new Random();

            while (!ct.IsCancellationRequested)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * (maxTemperature - minTemperature);
                double currentBloodPressure = minBloodPressure + rand.NextDouble() * (maxBloodPressure - minBloodPressure);
                double currentHeartRate = minHeartRate + rand.NextDouble() * (maxHeartRate - minHeartRate);

                // Create JSON message
                string messageBody = JsonSerializer.Serialize(
                    new
                {
                    temperature = currentTemperature,
                    bloodpressure = currentBloodPressure,
                    heartrate = currentHeartRate
                });
            using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
            };

            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            message.Properties.Add("temperatureAlert", (currentTemperature > maxTemperature) ? "true" : "false");
            message.Properties.Add("bloodpressureAlert", (currentBloodPressure < minBloodPressure || currentBloodPressure > maxBloodPressure) ? "true" : "false");
            message.Properties.Add("heartrateAlert", (currentHeartRate < minHeartRate || currentHeartRate > maxHeartRate) ? "true" : "false");

            // Send the telemetry message
            await s_deviceClient.SendEventAsync(message);
            Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");

            await Task.Delay(3000);
        }
    }
    }
}