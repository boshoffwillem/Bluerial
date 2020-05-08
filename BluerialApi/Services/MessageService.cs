using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BluerialApi.Services
{
    public class MessageService : IMessageService
    {
        #region Private Members
        private ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        private string _queue;
        #endregion

        #region Events
        /// <summary>
        /// Triggers when a message was received on <see cref="_queue"/>
        /// </summary>
        public event EventHandler<RabbitMQ.Client.Events.BasicDeliverEventArgs> MessageReceived = (model, args) => { };
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="queue">Name of queue that this instance will use</param>
        /// <param name="listenForReceived">Will this instance respond to received messages</param>
        public MessageService(string queue, bool listenForReceived)
        {
            Console.WriteLine("about to connect to rabbit");

            _factory = new ConnectionFactory() { HostName = "localhost" };
            //_factory = new ConnectionFactory() { HostName = "rabbitmq", Port = 5672 };
            _factory.UserName = "guest";
            _factory.Password = "guest";
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _queue = queue;
            _channel.QueueDeclare(queue: _queue,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            if (listenForReceived)
            {
                // Create a consumer
                EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

                // Callback for received/consumed messages
                consumer.Received += MessageReceived;

                // Start consumer
                _channel.BasicConsume(queue: "serial-service-consumer",
                                     autoAck: true,
                                     consumer: consumer);
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Method to send a message to <see cref="_queue"/>
        /// </summary>
        /// <param name="messageString">Message to send</param>
        /// <returns>Whether send was successful</returns>
        public bool Enqueue(string messageString)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes("server processed " + messageString);
                _channel.BasicPublish(exchange: "",
                                    routingKey: _queue,
                                    basicProperties: null,
                                    body: body);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}