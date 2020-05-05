using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace BLE
{
    class Program
    {
        static void Main()
        {
            // Create RabbitMQ consumer for ble messages
            ConnectionFactory bleMessagesFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection bleMessagesConnection = bleMessagesFactory.CreateConnection();
            using IModel bleMessagesChannel = bleMessagesConnection.CreateModel();

            // Create/Use ble-messages queue
            bleMessagesChannel.QueueDeclare(queue: "ble-messages",
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
                string messageType = message.Split(':')[0];

                // This will change console font color base on the type of message
                switch(messageType)
                {
                    case "New device":
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case "Device name changed":
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case "Device timeout":
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case "Device data changed":
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                Console.WriteLine(message);
            };

            // Start consumer
            bleMessagesChannel.BasicConsume(queue: "ble-messages",
                                 autoAck: true,
                                 consumer: bleMessagesConsumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        } 
    }
}
