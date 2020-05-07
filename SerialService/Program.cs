using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO.Ports;
using System.Text;

namespace SerialService
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Serial service started");

            // Create RabbitMQ channel for serial service
            ConnectionFactory serialServiceFactory = new ConnectionFactory() { HostName = "localhost" };
            using IConnection serialServiceConnection = serialServiceFactory.CreateConnection();
            using IModel serialServiceChannel = serialServiceConnection.CreateModel();

            SerialPortModel serialPortModel = new SerialPortModel(null, null);

            #region Events
            // Triggered when data is sent
            serialPortModel.DataSent += () =>
            {
                string message = "serial-data-sent-###xx";
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "ble-service-producer",
                                     basicProperties: null,
                                     body: body);
            };

            // Triggered when data is received
            serialPortModel.DataReceived += () =>
            {
                string message = "serial-data-received-###xx";
                byte[] body = Encoding.UTF8.GetBytes(message);

                // Produce message
                serialServiceChannel.BasicPublish(exchange: "",
                                     routingKey: "ble-service-producer",
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
                                     routingKey: "ble-service-producer",
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
                                     routingKey: "ble-service-producer",
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
                                     routingKey: "ble-service-producer",
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

                            if (!serialPortModel.IsOpen)
                                serialPortModel.OpenPort(comPort: comPort, baudRate: baudRate, 
                                    parity: parity, dataBits: dataBits, stopBits: stopBits);
                            break;

                        case "close": // Close the serial port
                            if (serialPortModel.IsOpen)
                                serialPortModel.ClosePort();
                            break;

                        case "stx": // Set the start characters of the frame
                            parameters = message.Split("###")[1];
                            string[] stxStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] stxbytes = Array.ConvertAll(stxStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            serialPortModel.STX = stxbytes;
                            break;
                                                
                        case "etx": // Set the end characters of the frame
                            parameters = message.Split("###")[1];
                            string[] etxStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] etxBytes = Array.ConvertAll(etxStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            serialPortModel.ETX = etxBytes;
                            break;
                            
                        case "message": // A message to be sent
                            parameters = message.Split("###")[1];
                            string[] messageStringBytes = parameters.Replace(" ", string.Empty).Trim().Split(',');
                            byte[] messageBytes = Array.ConvertAll(messageStringBytes, element =>
                            {
                                return byte.Parse(element, System.Globalization.NumberStyles.HexNumber);
                            });
                            serialPortModel.WriteSerialData(messageBytes);
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

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
