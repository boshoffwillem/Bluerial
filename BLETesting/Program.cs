using BLE;
using System;

namespace BLETesting
{
    class Program
    {
        static void Main(string[] args)
        {
            var watcher = new BLEAdvertisementWatcher();

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
                Console.WriteLine($"New device: {device}");
            };

            watcher.DeviceNameChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Device name changed: {device}");
            };

            watcher.DeviceTimeout += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Device timeout {device}");
            };

            watcher.StartListening();

            while (true)
            {
                // Pause until we press enter
                Console.ReadLine();

                // Get discovere devices
                var devices = watcher.DiscoveredDevices;

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{devices.Count} devices discovered...");

                foreach (var device in devices)
                    Console.WriteLine(device);
            }
        }
    }
}
