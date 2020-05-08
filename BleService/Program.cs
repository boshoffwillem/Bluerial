using System;
using System.Threading;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Text;
using RabbitMQ.Client.Events;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace BleService
{
    class Program
    {
        static void Main()
        {
            System.Console.WriteLine("Starting BLE service");
            Thread.Sleep(20000); // Wait 10 seconds for RabbitMQ to startup
            Console.WriteLine("BLE service started");

            List<string> deviceFilters = new List<string>();
            var watcher = new BLEAdvertisementWatcher(new GattServiceIds());

            // Build RabbitMQ channel for BLE service
            ConnectionFactory bleMessagesFactory = new ConnectionFactory() { HostName = "rabbitmq", Port = 5672 };
            using IConnection bleMessagesConnection = bleMessagesFactory.CreateConnection();
            using IModel bleMessagesChannel = bleMessagesConnection.CreateModel();

            #region RabbitMQ Producer for BLE Service
            // Create/Use queue
            bleMessagesChannel.QueueDeclare(queue: "ble-service-producer",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
            #endregion

            #region RabbitMQ Consumer for BLE Service
            // Create/Use queue
            bleMessagesChannel.QueueDeclare(queue: "ble-service-consumer",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            // Create a consumer
            EventingBasicConsumer bleMessagesConsumer = new EventingBasicConsumer(bleMessagesChannel);

            // Callback for received/consumed messages
            bleMessagesConsumer.Received += (model, ea) =>
            {
                ReadOnlyMemory<byte> body = ea.Body;
                string message = Encoding.UTF8.GetString(body.ToArray());

                if (message.StartsWith("ble-"))
                {
                    string command = message.Split('-')[1];

                    switch (command)
                    {
                        case "start": // Start listening
                            if (!watcher.Listening)
                                watcher.StartListening();
                            break;
                        case "stop": // Stop listening
                            if (watcher.Listening)
                                watcher.StopListening();
                            break;
                        case "devices": // Get discovered devices
                            SendDevicesMessage(bleMessagesChannel, watcher.DiscoveredDevices);                            
                            break;
                        case "clear": // Clear device filters
                            deviceFilters = new List<string>();
                            break;
                        case "add": // Add filter
                            string filter = message.Split("###")[1];

                            if (!deviceFilters.Contains(filter))
                                // Add new filter
                                deviceFilters.Add(filter);
                            break;
                        case "filters":
                            SendActiveFilters(bleMessagesChannel, deviceFilters);
                            break;
                        default:
                            break;
                    } 
                }
            };

            // Start consumer
            bleMessagesChannel.BasicConsume(queue: "ble-service-consumer",
                                 autoAck: true,
                                 consumer: bleMessagesConsumer);
            #endregion

            #region Events
            watcher.StartedListening += () =>
                {
                    StartScanning(bleMessagesChannel);
                };

            watcher.StoppedListening += () =>
            {
                StopScanning(bleMessagesChannel);
            };

            watcher.NewDeviceDiscovered += (device) =>
            {
                string message = $"New device: {device}";

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    SendMessage(bleMessagesChannel, message);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        SendMessage(bleMessagesChannel, message);
                    }
                }
            };

            watcher.DeviceNameChanged += (device) =>
            {
                // Build message to produce
                string message = $"Device name changed: {device}";

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    SendMessage(bleMessagesChannel, message);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        SendMessage(bleMessagesChannel, message);
                    }
                }
            };

            watcher.DeviceTimeout += (device) =>
            {
                // Build message to produce
                string message = $"Device timeout: {device}";

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    SendMessage(bleMessagesChannel, message);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        SendMessage(bleMessagesChannel, message);
                    }
                }
            };

            watcher.DeviceDataChanged += (device) =>
            {
                // Build message to produce
                string message = $"Device data changed: {device}";

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    SendMessage(bleMessagesChannel, message);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        SendMessage(bleMessagesChannel, message);
                    }
                }
            }; 
            #endregion

            watcher.StartListening();

            while(true);
        }

        /// <summary>
        /// The function post data back to the main Web Api
        /// </summary>
        /// <param name="postData">Data to post to Web Api</param>
        public static async Task PostMessage(string postData)
        {
            var json = JsonConvert.SerializeObject(postData);
            var content = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {
                    var result = await client.PostAsync("https://localhost:5001/commands", content);
                    string resultContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine("Server returned: " + resultContent);
                }
            }
        }

        /// <summary>
        /// Send message that scanning has started
        /// </summary>
        /// <param name="bleMessagesChannel">RabbitMQ channel to use</param>
        private static void StartScanning(IModel bleMessagesChannel)
        {
            // Build message to produce
            string message = $"ble-scan-started";
            // format message
            byte[] body = Encoding.UTF8.GetBytes(message);

            // Produce message
            bleMessagesChannel.BasicPublish(exchange: "",
                                 routingKey: "ble-service-producer",
                                 basicProperties: null,
                                 body: body);
            
            PostMessage(message).Wait();
        }

        /// <summary>
        /// Send message that scanning has stopped
        /// </summary>
        /// <param name="bleMessagesChannel">RabbitMQ channel to use</param>
        private static void StopScanning(IModel bleMessagesChannel)
        {
            // Build message to produce
            string message = $"ble-scan-stopped";
            // format message
            byte[] body = Encoding.UTF8.GetBytes(message);

            // Produce message
            bleMessagesChannel.BasicPublish(exchange: "",
                                 routingKey: "ble-service-producer",
                                 basicProperties: null,
                                 body: body);

            PostMessage(message).Wait();
        }

        /// <summary>
        /// Send list of devices discovered so far
        /// </summary>
        /// <param name="bleMessagesChannel">RabbitMQ channel to use</param>
        /// <param name="devices">List of devices discovered so far</param>
        private static void SendDevicesMessage(IModel bleMessagesChannel, IReadOnlyCollection<BleDevice> devices)
        {
            string devicesDiscovered = "";

            foreach (var device in devices)
                devicesDiscovered += "\t" + device + "\n\n";

            // Build message to produce
            string message = $"ble-devices-###\n{devicesDiscovered}";
            // format message
            byte[] body = Encoding.UTF8.GetBytes(message);

            // Produce message
            bleMessagesChannel.BasicPublish(exchange: "",
                                 routingKey: "ble-service-producer",
                                 basicProperties: null,
                                 body: body);

            PostMessage(message).Wait();
        }

        /// <summary>
        /// Send info regarding a device
        /// </summary>
        /// <param name="bleMessagesChannel">RabbitMQ channel to use</param>
        /// <param name="message">Message to send</param>
        private static void SendMessage(IModel bleMessagesChannel, string message)
        {
            // Build message to produce
            message = $"ble-message-###{message}" ;
            // format message
            var body = Encoding.UTF8.GetBytes(message);

            // Produce message
            bleMessagesChannel.BasicPublish(exchange: "",
                                 routingKey: "ble-service-producer",
                                 basicProperties: null,
                                 body: body); 

            PostMessage(message).Wait();         
        }

        /// <summary>
        /// Send list of active device filters
        /// </summary>
        /// <param name="bleMessagesChannel">RabbitMQ channel to use</param>
        /// <param name="deviceFilters">List of device filters</param>
        private static void SendActiveFilters(IModel bleMessagesChannel, List<string> deviceFilters)
        {
            string message = "ble-filters-###";

            foreach (string filter in deviceFilters)
            {
                message += $"Filter: {filter}\n";
            }

            // format message
            var body = Encoding.UTF8.GetBytes(message);

            // Produce message
            bleMessagesChannel.BasicPublish(exchange: "",
                                 routingKey: "ble-service-producer",
                                 basicProperties: null,
                                 body: body);

            PostMessage(message).Wait();
        }
    }
}
