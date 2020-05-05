using BLE;
using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Text;

namespace BLEComms
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> deviceFilters = new List<string>();
            var watcher = new BLEAdvertisementWatcher(new GattServiceIds());

            // Build RabbitMQ producer
            ConnectionFactory bleMessagesFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection bleMessagesConnection = bleMessagesFactory.CreateConnection();
            using IModel bleMessagesChannel = bleMessagesConnection.CreateModel();

            // Create/Use ble-messages queue
            bleMessagesChannel.QueueDeclare(queue: "ble-messages",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);            

            watcher.StartedListening += () =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Started listening");
            };

            watcher.StoppedListening += () =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Stopped listening");
            };

            watcher.NewDeviceDiscovered += (device) =>
            {
                // Build message to produce
                string message = $"New device: {device}";
                // format message
                var body = Encoding.UTF8.GetBytes(message);

                Console.ForegroundColor = ConsoleColor.Green;

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    // Listen to all devices
                    Console.WriteLine(message);

                    // Produce message
                    bleMessagesChannel.BasicPublish(exchange: "",
                                         routingKey: "ble-messages",
                                         basicProperties: null,
                                         body: body);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        Console.WriteLine(message);
                     
                        // Produce message
                        bleMessagesChannel.BasicPublish(exchange: "",
                                             routingKey: "ble-messages",
                                             basicProperties: null,
                                             body: body);
                    }
                }                    
            };

            watcher.DeviceNameChanged += (device) =>
            {
                // Build message to produce
                string message = $"Device name changed: {device}";
                // format message
                var body = Encoding.UTF8.GetBytes(message);

                Console.ForegroundColor = ConsoleColor.Blue;

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    // Listen to all devices
                    Console.WriteLine(message);

                    // Produce message
                    bleMessagesChannel.BasicPublish(exchange: "",
                                         routingKey: "ble-messages",
                                         basicProperties: null,
                                         body: body);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        Console.WriteLine(message);

                        // Produce message
                        bleMessagesChannel.BasicPublish(exchange: "",
                                             routingKey: "ble-messages",
                                             basicProperties: null,
                                             body: body);
                    }
                }
            };

            watcher.DeviceTimeout += (device) =>
            {
                // Build message to produce
                string message = $"Device timeout: {device}";
                // format message
                var body = Encoding.UTF8.GetBytes(message);

                Console.ForegroundColor = ConsoleColor.Red;

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    // Listen to all devices
                    Console.WriteLine(message);

                    // Produce message
                    bleMessagesChannel.BasicPublish(exchange: "",
                                         routingKey: "ble-messages",
                                         basicProperties: null,
                                         body: body);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        Console.WriteLine(message);

                        // Produce message
                        bleMessagesChannel.BasicPublish(exchange: "",
                                             routingKey: "ble-messages",
                                             basicProperties: null,
                                             body: body);
                    }
                }
            };

            watcher.DeviceDataChanged += (device) =>
            {
                // Build message to produce
                string message = $"Device data changed: {device}";
                // format message
                var body = Encoding.UTF8.GetBytes(message);

                Console.ForegroundColor = ConsoleColor.Blue;

                // If no filters...
                if (deviceFilters.Count == 0)
                {
                    // Listen to all devices
                    Console.WriteLine(message);

                    // Produce message
                    bleMessagesChannel.BasicPublish(exchange: "",
                                         routingKey: "ble-messages",
                                         basicProperties: null,
                                         body: body);
                }
                else
                {
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                    {
                        Console.WriteLine(message);

                        // Produce message
                        bleMessagesChannel.BasicPublish(exchange: "",
                                             routingKey: "ble-messages",
                                             basicProperties: null,
                                             body: body);
                    }
                }
            };
           
            watcher.StartListening();

            while (true)
            {
                // Pause until we press enter
                string command = Console.ReadLine();

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
                        var devices = watcher.DiscoveredDevices;

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"{devices.Count} devices discovered...");

                        foreach (var device in devices)
                            Console.WriteLine(device);
                        break;
                    case "clear": // Clear device filters
                        deviceFilters = new List<string>();
                        break;
                    case "blank": // Clear console screen
                        Console.Clear();
                        break;
                    default: // If none of the above...
                        // If "add-XX" command...
                        if (command.Contains("add-"))
                        {
                            string filter = command.Split('-')[1].ToUpper();
                            // Add new filter
                            deviceFilters.Add(filter);
                        }
                        break;
                }
            }
        }
    }
}
