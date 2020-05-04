using BLE;
using System;

namespace BLETesting
{
    class Program
    {
        static void Main()
        {
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
                Console.WriteLine($"New device: {device}");

                if (device.Address.ToString("X").Contains("80"))
                    bleToSerialPiper.WriteSerialData(device.Data);
            };

            watcher.DeviceNameChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Device name changed: {device}");

                if (device.Address.ToString("X").Contains("80"))
                    bleToSerialPiper.WriteSerialData(device.Data);
            };

            watcher.DeviceTimeout += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Device timeout {device}");
            };

            watcher.DeviceDataChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Device data changed: {device}");

                if (device.Address.ToString("X").Contains("80"))
                    bleToSerialPiper.WriteSerialData(device.Data);
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
                Console.ReadLine();

                // Get discover devices
                var devices = watcher.DiscoveredDevices;

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{devices.Count} devices discovered...");

                foreach (var device in devices)
                    Console.WriteLine(device);
            }
        }

        
    }
}
