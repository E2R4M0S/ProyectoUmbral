using Microsoft.EntityFrameworkCore;

namespace Trivia.Infrastructure;

public class TriviaDbContext : DbContext
{
    public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options)
    {
    }
}
