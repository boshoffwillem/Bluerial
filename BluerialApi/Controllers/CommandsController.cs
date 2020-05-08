using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluerialApi.Models;
using BluerialApi.Services;
using RabbitMQ.Client.Events;

namespace BluerialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommandsController : ControllerBase
    {
        #region Private Members
        /// <summary>
        /// This will be the RabbitMQ channel to produce messages
        /// to the serial-service-consumer queue
        /// </summary>
        private readonly IMessageService _serialServiceConsumer;

        /// <summary>
        /// This will be the RabbitMQ channel to produce messages
        /// to the serial-service-producer queue
        /// </summary>
        private readonly IMessageService _serialServiceProducer;

        /// <summary>
        /// This will be the RabbitMQ channel to produce messages
        /// to the ble-service-consumer queue
        /// </summary>
        private readonly IMessageService _bleServiceConsumer;

        /// <summary>
        /// This will be the RabbitMQ channel to produce messages
        /// to the ble-service-producer queue
        /// </summary>
        private readonly IMessageService _bleServiceProducer;

        private readonly CommandContext _context;
        #endregion
    
        public CommandsController(CommandContext context)
        {
            _context = context;
            _serialServiceConsumer = new MessageService("serial-service-consumer", false);
            _serialServiceProducer = new MessageService("serial-service-producer", true);
            _serialServiceProducer.MessageReceived += MessageReceived;
            _bleServiceConsumer = new MessageService("ble-service-consumer", false);
            _bleServiceProducer = new MessageService("ble-service-producer", true);
            _bleServiceProducer.MessageReceived += MessageReceived;
        }

        #region REST apis
        // GET: api/Commands
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Command>>> GetCommandsList()
        {
            return await _context.CommandsList.ToListAsync();
        }

        // GET: api/Commands/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Command>> GetCommand(long id)
        {
            var command = await _context.CommandsList.FindAsync(id);

            if (command == null)
            {
                return NotFound();
            }

            return command;
        }

        // PUT: api/Commands/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCommand(long id, Command command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            _context.Entry(command).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommandExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Commands
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Command>> PostCommand(Command command)
        {
            _context.CommandsList.Add(command);
            await _context.SaveChangesAsync();

            if (command.Name.StartsWith("serial-"))
            {
                if (_serialServiceConsumer != null)
                {
                    _serialServiceConsumer.Enqueue(command.CommandToSend());
                }
            }
            else if (command.Name.StartsWith("ble-"))
            {
                if (_bleServiceConsumer != null)
                {
                    _bleServiceConsumer.Enqueue(command.CommandToSend());
                }
            }

            //return CreatedAtAction("GetCommand", new { id = command.Id }, command);
            return CreatedAtAction(nameof(GetCommand), new { id = command.Id }, command);
        }

        // DELETE: api/Commands/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Command>> DeleteCommand(long id)
        {
            var command = await _context.CommandsList.FindAsync(id);
            if (command == null)
            {
                return NotFound();
            }

            _context.CommandsList.Remove(command);
            await _context.SaveChangesAsync();

            return command;
        }
        #endregion

        #region Helper Functions
        private bool CommandExists(long id)
        {
            return _context.CommandsList.Any(e => e.Id == id);
        }

        /// <summary>
        /// Fired when a message is received on a RabbitMQ queue
        /// that is being listened to
        /// </summary>
        /// <param name="sender">The sending queue</param>
        /// <param name="args">Data of the queue</param>
        private void MessageReceived(object sender, BasicDeliverEventArgs args)
        {
            System.Console.WriteLine($"Received: {args.Body}");
        }
        #endregion
    }
}
