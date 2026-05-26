using Microsoft.EntityFrameworkCore;

namespace Sessions.Infrastructure;

public class SessionsDbContext : DbContext
{
    public SessionsDbContext(DbContextOptions<SessionsDbContext> options) : base(options)
    {
    }
}
