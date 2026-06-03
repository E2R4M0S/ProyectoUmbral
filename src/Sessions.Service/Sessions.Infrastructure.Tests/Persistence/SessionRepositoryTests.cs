using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Sessions.Infrastructure.Persistence;
using Xunit;

namespace Sessions.Infrastructure.Tests.Persistence;

public class SessionRepositoryTests
{
    private static SessionsDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SessionsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SessionsDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistSession()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new SessionRepository(dbContext);

        var session = Session.Create("Test Session", Guid.NewGuid(), "Test Mission", "123456");

        // Act
        await repo.AddAsync(session, CancellationToken.None);

        // Assert
        var saved = await dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == session.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Session");
        saved.Pin.Should().Be("123456");
        saved.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Fact]
    public async Task IsPinUniqueAsync_WhenPinIsFree_ReturnsTrue()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.IsPinUniqueAsync("999999", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPinUniqueAsync_WhenPinExists_ReturnsFalse()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var existingSession = Session.Create("Existing", Guid.NewGuid(), "Test Mission", "111111");
        dbContext.Sessions.Add(existingSession);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.IsPinUniqueAsync("111111", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new SessionRepository(dbContext);
        var beforeCreate = DateTime.UtcNow;

        var session = Session.Create("Timed Session", Guid.NewGuid(), "Test Mission", "222222");

        // Act
        await repo.AddAsync(session, CancellationToken.None);
        var afterCreate = DateTime.UtcNow;

        // Assert
        session.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        session.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task IsPinUniqueAsync_WithMultipleSessions_ChecksCorrectly()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session1 = Session.Create("Session 1", Guid.NewGuid(), "Test Mission", "333333");
        var session2 = Session.Create("Session 2", Guid.NewGuid(), "Test Mission", "444444");
        dbContext.Sessions.AddRange(session1, session2);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act & Assert
        (await repo.IsPinUniqueAsync("333333", CancellationToken.None)).Should().BeFalse();
        (await repo.IsPinUniqueAsync("444444", CancellationToken.None)).Should().BeFalse();
        (await repo.IsPinUniqueAsync("555555", CancellationToken.None)).Should().BeTrue();
    }
}
