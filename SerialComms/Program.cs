using BLE;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO.Ports;
using System.Linq.Expressions;
using System.Text;

namespace SerialComms
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Serial service started");

            #region Create Serial Service
            BleToSerialPiper bleToSerialPiper = new BleToSerialPiper(null, null); 
            #endregion

            #region Events
            // Triggered when data is sent
            bleToSerialPiper.DataSent += () =>
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            };

            // Triggered when data is received
            bleToSerialPiper.DataReceived += () =>
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            };

            // Triggered when port is opened
            bleToSerialPiper.OpenedPort += () =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
            };

            // Triggered when port is closed
            bleToSerialPiper.ClosedPort += () =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            };

            // Triggered when there is an error on the port
            bleToSerialPiper.PortError += (sender, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
            };
            #endregion

            #region Create RabbitMQ Producer
            #endregion

            #region Create RabbitMQ Consumer
            // Create RabbitMQ consumer for serial messages
            ConnectionFactory serialServiceFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection serialServiceConnection = serialServiceFactory.CreateConnection();
            using IModel serialServiceChannel = serialServiceConnection.CreateModel();

            // Create/Use serial-messages queue
            serialServiceChannel.QueueDeclare(queue: "serial-service-slave",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            // Create a consumer
            EventingBasicConsumer serialServiceConsumer = new EventingBasicConsumer(serialServiceChannel); 
            
            // Callback for received/consumed messages
            serialServiceConsumer.Received += (model, ea) =>
            {
                ReadOnlyMemory<byte> body = ea.Body;
                string message = Encoding.UTF8.GetString(body.ToArray());

                if (message.StartsWith("serial-"))
                {
                    // The type of message
                    string[] commands = message.Split('-');

                    // Data carried by the message
                    string parameters;

                    switch (commands[1])
                    {
                        case "open": // Open the serial port
                            parameters = message.Split("###")[1].Replace(" ", string.Empty).Trim();
                            string[] arguments = parameters.Split(',');
                            byte comPort = 0;
                            int baudRate = 9600;
                            Parity parity = Parity.None;
                            int dataBits = 8;
                            StopBits stopBits = StopBits.One;

                            foreach (string arg in arguments)
                            {
                                string[] components = arg.Split(':');
                                switch(components[0])
                                {
                                    case "comPort":
                                        comPort = byte.Parse(components[1]);
                                        break;
                                    case "baudRate":
                                        baudRate = int.Parse(components[1]);
                                        break;
                                    case "parity":
                                        string parityOption = components[1];
                                        switch(parityOption)
                                        {
                                            case "Even":
                                                parity = Parity.Even;
                                                break;
                                            case "Mark":
                                                parity = Parity.Mark;
                                                break;
                                            case "None":
                                                parity = Parity.None;
                                                break;
                                            case "Odd":
                                                parity = Parity.Odd;
                                                break;
                                            case "Space":
                                                parity = Parity.Space;
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    case "dataBits":
                                        dataBits = int.Parse(components[1]);
                                        break;
                                    case "stopBits":
                                        string stopBitsOption = components[1];
                                        switch (stopBitsOption)
                                        {
                                            case "None":
                                                stopBits = StopBits.None;
                                                break;
                                            case "One":
                                                stopBits = StopBits.One;
                                                break;
                                            case "OnePointFive":
                                                stopBits = StopBits.OnePointFive;
                                                break;
                                            case "Two":
                                                stopBits = StopBits.Two;
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }

                            bleToSerialPiper.OpenPort(comPort: comPort, baudRate: baudRate, 
                                parity: parity, dataBits: dataBits, stopBits: stopBits);
                            break;

                        case "close": // Close the serial port
                            bleToSerialPiper.ClosePort();
                            break;

                        case "stx": // Set the start characters of the frame
                            parameters = message.Split("###")[1];
                            string[] stxStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] stxbytes = Array.ConvertAll(stxStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            bleToSerialPiper.STX = stxbytes;
                            break;
                                                
                        case "etx": // Set the end characters of the frame
                            parameters = message.Split("###")[1];
                            string[] etxStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] etxBytes = Array.ConvertAll(etxStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            bleToSerialPiper.ETX = etxBytes;
                            break;
                            
                        case "message": // A message to be sent
                            parameters = message.Split("###")[1];
                            string[] messageStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] messageBytes = Array.ConvertAll(messageStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            bleToSerialPiper.WriteSerialData(messageBytes);
                            break;

                        default:
                            break;
                    }
                }                             
            };

            // Start consumer
            serialServiceChannel.BasicConsume(queue: "serial-service-slave",
                                 autoAck: true,
                                 consumer: serialServiceConsumer);
            #endregion

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
