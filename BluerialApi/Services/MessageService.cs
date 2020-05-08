using System;
using System.Text;
using RabbitMQ.Client;

namespace BluerialApi.Services
{
    // define interface and service
    public interface IMessageService
    {
        bool Enqueue(string message);
    }

    public class MessageService : IMessageService
    {
        #region Private Members
        private ConnectionFactory mFactory;
        private IConnection mConnection;
        private IModel mChannel;
        private string mQueue;
        #endregion

        #region Events
        public event Action MessageReceived = () => { };
        #endregion

        #region Constructor

        public MessageService(string queue)
        {
            Console.WriteLine("about to connect to rabbit");

            mFactory = new ConnectionFactory() { HostName = "localhost" };
            //mFactory = new ConnectionFactory() { HostName = "rabbitmq", Port = 5672 };
            mFactory.UserName = "guest";
            mFactory.Password = "guest";
            mConnection = mFactory.CreateConnection();
            mChannel = mConnection.CreateModel();
            mQueue = queue;
            mChannel.QueueDeclare(queue: mQueue,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
        }
        #endregion

        public bool Enqueue(string messageString)
        {
            var body = Encoding.UTF8.GetBytes("server processed " + messageString);
            mChannel.BasicPublish(exchange: "",
                                routingKey: mQueue,
                                basicProperties: null,
                                body: body);
            return true;
        }
    }
}