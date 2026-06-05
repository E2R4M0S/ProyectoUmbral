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

    [Fact]
    public async Task GetByIdAsync_WhenSessionExists_ReturnsSessionWithParticipants()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Existing Session", Guid.NewGuid(), "Test Mission", "666666");
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByIdAsync(session.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Existing Session");
        result.MissionId.Should().Be(session.MissionId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPinAsync_WhenPinExists_ReturnsSession()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Pin Session", Guid.NewGuid(), "Test Mission", "777777");
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByPinAsync("777777", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Pin.Should().Be("777777");
    }

    [Fact]
    public async Task GetByPinAsync_WhenPinDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByPinAsync("000000", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WhenNameExists_ReturnsSession()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Named Session", Guid.NewGuid(), "Test Mission", "888888");
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByNameAsync("Named Session", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Named Session");
    }

    [Fact]
    public async Task GetByNameAsync_WhenNameDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByNameAsync("Non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionsAsync_WithNoFilters_ReturnsAllSessionsPaginated()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var missionId = Guid.NewGuid();
        var sessions = new[]
        {
            Session.Create("Session A", missionId, "Mission", "111111"),
            Session.Create("Session B", missionId, "Mission", "222222"),
            Session.Create("Session C", missionId, "Mission", "333333"),
        };
        dbContext.Sessions.AddRange(sessions);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetSessionsAsync(null, null, null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(3);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSessionsAsync_WithSearchFilter_ReturnsMatchingSessions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var missionId = Guid.NewGuid();
        dbContext.Sessions.Add(Session.Create("Alpha Session", missionId, "Mission", "444444"));
        dbContext.Sessions.Add(Session.Create("Beta Session", missionId, "Mission", "555555"));
        dbContext.Sessions.Add(Session.Create("Alpha Beta Session", missionId, "Mission", "666666"));
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetSessionsAsync("alpha", null, null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(2);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSessionsAsync_WithMissionIdFilter_ReturnsMatchingSessions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var missionId1 = Guid.NewGuid();
        var missionId2 = Guid.NewGuid();
        dbContext.Sessions.Add(Session.Create("Session 1", missionId1, "Mission 1", "111111"));
        dbContext.Sessions.Add(Session.Create("Session 2", missionId2, "Mission 2", "222222"));
        dbContext.Sessions.Add(Session.Create("Session 3", missionId1, "Mission 1", "333333"));
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetSessionsAsync(null, null, missionId1, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(2);
        result.Should().HaveCount(2);
        result.All(s => s.MissionId == missionId1).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistSessionChanges()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Original Name", Guid.NewGuid(), "Original Mission", "999999");
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        session.TransitionTo(SessionStatus.Preparing);
        await repo.UpdateAsync(session, CancellationToken.None);

        // Assert
        var saved = await dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == session.Id);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(SessionStatus.Preparing);
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldPersistParticipant()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Participant Session", Guid.NewGuid(), "Test Mission", "000001");
        session.TransitionTo(SessionStatus.Preparing);
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);
        var participant = SessionParticipant.Create(session.Id, Guid.NewGuid());

        // Act
        await repo.AddParticipantAsync(participant, CancellationToken.None);

        // Assert
        var saved = await dbContext.Set<SessionParticipant>()
            .FirstOrDefaultAsync(p => p.Id == participant.Id);
        saved.Should().NotBeNull();
        saved!.SessionId.Should().Be(session.Id);
    }
}
