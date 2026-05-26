using Microsoft.EntityFrameworkCore;

namespace Teams.Infrastructure;

public class TeamsDbContext : DbContext
{
    public TeamsDbContext(DbContextOptions<TeamsDbContext> options) : base(options)
    {
    }
}
