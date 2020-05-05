using BLE;
using System;

namespace SerialComms
{
    class Program
    {
        static void Main(string[] args)
        {
            BleToSerialPiper bleToSerialPiper = new BleToSerialPiper(null, null);

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
        }
    }
}
