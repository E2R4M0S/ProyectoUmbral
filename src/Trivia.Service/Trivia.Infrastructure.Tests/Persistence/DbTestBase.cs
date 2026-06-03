using System;
using Microsoft.EntityFrameworkCore;
using Trivia.Infrastructure;

namespace Trivia.Infrastructure.Tests.Persistence;

public abstract class DbTestBase : IDisposable
{
    private readonly string _dbName;

    protected DbTestBase()
    {
        _dbName = Guid.NewGuid().ToString();
    }

    protected TriviaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        var ctx = new TriviaDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public void Dispose()
    {
        var options = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        using var ctx = new TriviaDbContext(options);
        ctx.Database.EnsureDeleted();
    }
}
