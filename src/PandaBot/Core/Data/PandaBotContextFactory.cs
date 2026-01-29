using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PandaBot.Core.Data;

public class PandaBotContextFactory : IDesignTimeDbContextFactory<PandaBotContext>
{
    public PandaBotContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PandaBotContext>();
        optionsBuilder.UseSqlite("Data Source=pandabot.db");

        return new PandaBotContext(optionsBuilder.Options);
    }
}
