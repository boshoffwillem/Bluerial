using BLE;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace SerialComms
{
    class Program
    {
        static void Main()
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

            // Create RabbitMQ consumer for serial messages
            ConnectionFactory serialMessagesFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection serialMessagesConnection = serialMessagesFactory.CreateConnection();
            using IModel serialMessagesChannel = serialMessagesConnection.CreateModel();

            // Create/Use serial-messages queue
            serialMessagesChannel.QueueDeclare(queue: "serial-messages",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            // Create a consumer
            EventingBasicConsumer serialMessagesConsumer = new EventingBasicConsumer(serialMessagesChannel);

            // Callback for received/consumed messages
            serialMessagesConsumer.Received += (model, ea) =>
            {
                ReadOnlyMemory<byte> body = ea.Body;
                string message = Encoding.UTF8.GetString(body.ToArray());
                
                if (message.Contains("OpenPort")) // for example, OpenPort(10, 9600)
                {
                    string[] parameters = message.Split('(')[1].Trim(new char[] { ')', ' ' }).Split(',');
                    byte port = byte.Parse(parameters[0]);
                    int baud = int.Parse(parameters[1]);
                    bleToSerialPiper.OpenPort(port, baud);
                }

                if (message.Contains("STX")) // for example, STX:02,0A
                {
                    string parameters = message.Split(':')[1];
                    string[] bytes = parameters.Split(',');
                    byte[] stx = Array.ConvertAll(bytes,
                        element => byte.Parse(element));
                    bleToSerialPiper.STX = stx;
                }

                if (message.Contains("STX")) // for example, ETX:02,0A
                {
                    string parameters = message.Split(':')[1];
                    string[] bytes = parameters.Split(',');
                    byte[] etx = Array.ConvertAll(bytes,
                        element => byte.Parse(element));
                    bleToSerialPiper.ETX = etx;
                }

                if (message.Contains("WriteSerialData")) // for example, WriteSerialData(0A,01,02)
                {
                    if (bleToSerialPiper.IsOpen)
                    {
                        string[] bytes = message.Split('(')[1].Trim(new char[] { ')', ' ' }).Split(',');
                        byte[] data = Array.ConvertAll(bytes,
                            element => byte.Parse(element));
                        bleToSerialPiper.WriteSerialData(data);
                    }
                }
            };

            // Start consumer
            serialMessagesChannel.BasicConsume(queue: "serial-messages",
                                 autoAck: true,
                                 consumer: serialMessagesConsumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
