using System;

namespace BluerialApi.Services
{
    public interface IMessageService
    {
        /// <summary>
        /// Requires a way to send messages
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <returns>If send was successful</returns>
        bool Enqueue(string message);

        /// <summary>
        /// Requires a way to receive messages
        /// </summary>
        event EventHandler<RabbitMQ.Client.Events.BasicDeliverEventArgs> MessageReceived;
    }
}