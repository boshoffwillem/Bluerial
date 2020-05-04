using BLE;
using System;
using System.Collections.Generic;

namespace BLETesting
{
    class Program
    {
        static void Main()
        {
            List<string> deviceFilters = new List<string>();
            var watcher = new BLEAdvertisementWatcher(new GattServiceIds());
            BleToSerialPiper bleToSerialPiper = new BleToSerialPiper(null, null);            

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
                Console.ForegroundColor = ConsoleColor.Green;

                // If no filters...
                if (deviceFilters.Count == 0)
                    // Listen to all devices
                    Console.WriteLine($"New device: {device}");
                else
                    // Only listen to specific devices
                    if (deviceFilters.Contains(device.DeviceId))
                        Console.WriteLine($"New device: {device}");

                //if (device.Address.ToString("X").Contains("80"))
                    //bleToSerialPiper.WriteSerialData(device.Data);
            };

            watcher.DeviceNameChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;

                if (deviceFilters.Count == 0)
                    Console.WriteLine($"Device name changed: {device}");
                else
                    if (deviceFilters.Contains(device.DeviceId))
                    Console.WriteLine($"Device name changed: {device}");

                //if (device.Address.ToString("X").Contains("80"))
                    //bleToSerialPiper.WriteSerialData(device.Data);
            };

            watcher.DeviceTimeout += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;

                if (deviceFilters.Count == 0)
                    Console.WriteLine($"Device timeout {device}");
                else
                    if (deviceFilters.Contains(device.DeviceId))
                    Console.WriteLine($"Device timeout {device}");
            };

            watcher.DeviceDataChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;

                if (deviceFilters.Count == 0)
                    Console.WriteLine($"Device data changed: {device}");
                else
                    if (deviceFilters.Contains(device.DeviceId))
                    Console.WriteLine($"Device data changed: {device}");

                //if (device.Address.ToString("X").Contains("80"))
                    //bleToSerialPiper.WriteSerialData(device.Data);
            };

            bleToSerialPiper.DataSent += () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Data frame sent!");
            };

            bleToSerialPiper.DataReceived += () =>
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Data received!");
            };

            bleToSerialPiper.OpenPort(10, 9600);
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
