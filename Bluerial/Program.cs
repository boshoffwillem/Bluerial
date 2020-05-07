using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Bluerial
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Main service started");

            #region RabbitMQ Setup
            // Create RabbitMQ channel for BLE service
            ConnectionFactory bleMessagesFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection bleMessagesConnection = bleMessagesFactory.CreateConnection();
            using IModel bleMessagesChannel = bleMessagesConnection.CreateModel();
            
            // Create RabbitMQ channel for Serial service
            ConnectionFactory serialMessagesFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection serialMessagesConnection = bleMessagesFactory.CreateConnection();
            using IModel serialMessagesChannel = bleMessagesConnection.CreateModel();
            #endregion

            #region RabbitMQ Producer for BLE Service
            // Creates queue
            bleMessagesChannel.QueueDeclare(queue: "ble-service-consumer",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
            #endregion

            #region RabbitMQ Consumer for BLE Service
            // Create/Use queue
            bleMessagesChannel.QueueDeclare(queue: "ble-service-producer",
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
                    // The type of message
                    string[] commands = message.Split('-');

                    // Data carried by the message
                    string parameters;

                    switch (commands[1])
                    {
                        case "scan":
                            switch (commands[2])
                            {
                                case "started":
                                    Console.WriteLine("BLE scanning has started");
                                    break;
                                case "stopped":
                                    Console.WriteLine("BLE scanning has stopped");
                                    break;
                                default:
                                    Console.WriteLine("Unknown BLE scan command");
                                    break;
                            }
                            break;
                        case "devices":
                            parameters = message.Split("###")[1];
                            Console.WriteLine($"List of devices: {parameters}");
                            break;
                        case "message":
                            parameters = message.Split("###")[1];
                            string messageType = parameters.Split(':')[0];

                            // This will change console font color base on the type of message
                            switch (messageType)
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
                            break;
                        case "filters":
                            Console.WriteLine(message);
                            break;
                        default:
                            break;
                    }
                }            
            };

            // Start consumer
            bleMessagesChannel.BasicConsume(queue: "ble-service-producer",
                                 autoAck: true,
                                 consumer: bleMessagesConsumer);
            #endregion

            #region RabbitMQ Producer for Serial Service
            // Creates queue
            serialMessagesChannel.QueueDeclare(queue: "serial-service-consumer",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
            #endregion

            #region RabbitMQ Consumer for Serial Service
            // Create a queue
            serialMessagesChannel.QueueDeclare(queue: "serial-service-producer",
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
                Console.WriteLine(message);
            };

            // Start consumer
            serialMessagesChannel.BasicConsume(queue: "serial-service-producer",
                                 autoAck: true,
                                 consumer: serialMessagesConsumer);
            #endregion

            while (true)
            {
                byte[] body;
                string command = Console.ReadLine();

                switch (command)
                {
                    case "clear":
                        Console.Clear();
                        break;
                    default:
                        string service = command.Split('-')[0];
                        switch (service)
                        {
                            case "ble":
                                // format message
                                body = Encoding.UTF8.GetBytes(command);

                                // Produce message
                                bleMessagesChannel.BasicPublish(exchange: "",
                                                     routingKey: "ble-service-consumer",
                                                     basicProperties: null,
                                                     body: body);
                                break;
                            case "serial":
                                // format message
                                body = Encoding.UTF8.GetBytes(command);

                                // Produce message
                                bleMessagesChannel.BasicPublish(exchange: "",
                                                     routingKey: "serial-service-consumer",
                                                     basicProperties: null,
                                                     body: body);
                                break;
                            default:
                                break;
                        }
                        break;
                }
 
            }
        } 
    }
}
