using Microsoft.EntityFrameworkCore;

namespace Missions.Infrastructure;

public class MissionsDbContext : DbContext
{
    public MissionsDbContext(DbContextOptions<MissionsDbContext> options) : base(options)
    {
    }
}
