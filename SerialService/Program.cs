using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Linq;

namespace SerialService
{
    class Program
    {
        static void Main()
        {
            System.Console.WriteLine("Starting Serial service");
            //Thread.Sleep(20000); // Wait 10 seconds for RabbitMQ to startup
            Console.WriteLine("Serial service started");

            // Create RabbitMQ channel for serial service
            ConnectionFactory serialServiceFactory = new ConnectionFactory() { HostName = "localhost" };
            //ConnectionFactory serialServiceFactory = new ConnectionFactory() { HostName = "rabbitmq", Port = 5672 };
            using IConnection serialServiceConnection = serialServiceFactory.CreateConnection();
            using IModel serialServiceChannel = serialServiceConnection.CreateModel();

            SerialPortModel serialPortModel = new SerialPortModel(null, null);

            #region Events
            // Triggered when data is sent
            serialPortModel.DataSent += (data) =>
            {
                string message = "serial-data-sent-###" + string.Join(",", data.Select(p => p.ToString("X")));
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "serial-service-producer",
                                     basicProperties: null,
                                     body: body);
            };

            // Triggered when data is received
            serialPortModel.DataReceived += (data) =>
            {
                
                string message = "serial-data-received-###" + string.Join(",", data.Select(p => p.ToString("X")));
                //string message = "serial-data-received-###" + string.Join(",", data.Select(p => p.ToString()).ToArray());
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "serial-service-producer",
                                     basicProperties: null,
                                     body: body);
            };

            // Triggered when port is opened
            serialPortModel.OpenedPort += () =>
            {
                string message = "serial-opened";
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "serial-service-producer",
                                     basicProperties: null,
                                     body: body);
            };

            // Triggered when port is closed
            serialPortModel.ClosedPort += () =>
            {
                string message = "serial-closed";
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "serial-service-producer",
                                     basicProperties: null,
                                     body: body);
            };

            // Triggered when there is an error on the port
            serialPortModel.PortError += (sender, args) =>
            {
                string message = "serial-error-###xxx";
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "serial-service-producer",
                                     basicProperties: null,
                                     body: body);
            };
            #endregion

            #region Create RabbitMQ Producer
            // Creates queue
            serialServiceChannel.QueueDeclare(queue: "serial-service-producer",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
            #endregion

            #region Create RabbitMQ Consumer
            // Create/Use serial-messages queue
            serialServiceChannel.QueueDeclare(queue: "serial-service-consumer",
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
                message = message.ToLower().Replace("server processed ", "");

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
                                    case "comport":
                                        comPort = byte.Parse(components[1]);
                                        break;
                                    case "baudrate":
                                        baudRate = int.Parse(components[1]);
                                        break;
                                    case "parity":
                                        string parityOption = components[1];
                                        switch(parityOption.ToLower())
                                        {
                                            case "even":
                                                parity = Parity.Even;
                                                break;
                                            case "mark":
                                                parity = Parity.Mark;
                                                break;
                                            case "none":
                                                parity = Parity.None;
                                                break;
                                            case "odd":
                                                parity = Parity.Odd;
                                                break;
                                            case "space":
                                                parity = Parity.Space;
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    case "databits":
                                        dataBits = int.Parse(components[1]);
                                        break;
                                    case "stopbits":
                                        string stopBitsOption = components[1];
                                        switch (stopBitsOption)
                                        {
                                            case "none":
                                                stopBits = StopBits.None;
                                                break;
                                            case "one":
                                                stopBits = StopBits.One;
                                                break;
                                            case "onepointfive":
                                                stopBits = StopBits.OnePointFive;
                                                break;
                                            case "two":
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

                            if (!serialPortModel.IsOpen)
                                serialPortModel.OpenPort(comPort: comPort, baudRate: baudRate, 
                                    parity: parity, dataBits: dataBits, stopBits: stopBits);
                            System.Console.WriteLine(message);
                            break;

                        case "close": // Close the serial port
                            if (serialPortModel.IsOpen)
                                serialPortModel.ClosePort();
                            System.Console.WriteLine(message);
                            break;

                        case "stx": // Set the start characters of the frame
                            parameters = message.Split("###")[1];
                            string[] stxStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] stxbytes = Array.ConvertAll(stxStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            serialPortModel.STX = stxbytes;
                            System.Console.WriteLine(message);
                            break;
                                                
                        case "etx": // Set the end characters of the frame
                            parameters = message.Split("###")[1];
                            string[] etxStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] etxBytes = Array.ConvertAll(etxStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            serialPortModel.ETX = etxBytes;
                            System.Console.WriteLine(message);
                            break;
                            
                        case "message": // A message to be sent
                            parameters = message.Split("###")[1];
                            string[] messageStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] messageBytes = Array.ConvertAll(messageStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            serialPortModel.WriteSerialData(messageBytes);
                            System.Console.WriteLine(message);
                            break;

                        default:
                            break;
                    }
                }                             
            };

            // Start consumer
            serialServiceChannel.BasicConsume(queue: "serial-service-consumer",
                                 autoAck: true,
                                 consumer: serialServiceConsumer);
            #endregion

            while (true) ;
        }
    }
}
