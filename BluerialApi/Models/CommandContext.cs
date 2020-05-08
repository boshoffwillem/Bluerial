using Microsoft.EntityFrameworkCore;

namespace BluerialApi.Models
{
    public class CommandContext : DbContext
    {
        public CommandContext(DbContextOptions<CommandContext> options)
            : base(options)
        {
        }

        public DbSet<Command> CommandsList { get; set; }
    }
}