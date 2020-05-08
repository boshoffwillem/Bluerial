using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BluerialApi.Services;
using BluerialApi.Models;

namespace BluerialApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CmdController : ControllerBase
    {
        #region Private Members
        /// <summary>
        /// This will be the RabbitMQ channel to produce messages
        /// to the serial-service-consumer queue
        /// </summary>
        private readonly IMessageService mSerialServiceConsumer;

        /// <summary>
        /// A list containing the supported commands
        /// </summary>
        /// <typeparam name="Command"><see cref="Command"/></typeparam>
        /// <returns></returns>
        private List<Command> mCommands = new List<Command>();
        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public CmdController()
        {
            mSerialServiceConsumer = new MessageService("serial-service-consumer");
            mCommands.Add(new Command {
                Name = "serial-open",
                Value = "comPort: 5, baudRate: 1200, parity: None, dataBits: 8, stopBits: One",
                Description = "\n\tOpens the specified serial port, eg. 'comPort: 5, baudRate: 9600, parity: None, dataBits: 8, stopBits: One'.\n" +
                "\t-- parity options = Even, Mark, None, Odd, Space\n" +
                "\t-- stopBit options = None, One, OnePointFive, Two\n"
            });
        }
       
        [HttpGet] // GET: api/commands
        /// <summary>
        /// Api to get all the supported commands that the user can input
        /// </summary>
        /// <returns>List of supported commands</returns>
        public List<Command> Get()
        {
            return mCommands;
        }

        // GET: api/commands/value
        [HttpGet]
        /// <summary>
        /// Api to get a documentation of a specific command
        /// </summary>
        /// <param name="value">The specific command</param>
        /// <returns>Documentation for the specific command</returns>
        public Command Get(string value)
        {
            return mCommands.Where(x => x.Name == value).FirstOrDefault();
        }

        // POST: api/commands
        [HttpPost]
        /// <summary>
        /// Api to send a command to a service
        /// </summary>
        /// <param name="payload">The command to be executed</param>
        public void Post([FromBody] string payload)
        {
            Console.WriteLine("received a Post: " + payload);
            mSerialServiceConsumer.Enqueue(payload);
        }

        // PUT: api/commands/id
        [HttpPut]
        /// <summary>
        /// Api to add a supported command
        /// </summary>
        /// <param name="id">The command</param>
        /// <param name="value">Documentation of the command</param>
        public void Put(string id, [FromBody]string value)
        {}

        // DELETE: api/commands/id
        [HttpPut]
        /// <summary>
        /// Api to delete a specific command
        /// </summary>
        /// <param name="id">Command to be deleted</param>
        public void Delete(int id)
        {}
    }
}
