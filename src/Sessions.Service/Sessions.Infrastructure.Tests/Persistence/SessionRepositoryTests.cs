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

        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });

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

        var existingSession = Session.Create("Existing", "111111", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
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

        var session = Session.Create("Timed Session", "222222", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });

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

        var session1 = Session.Create("Session 1", "333333", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
        var session2 = Session.Create("Session 2", "444444", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
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

        var session = Session.Create("Existing Session", "666666", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetByIdAsync(session.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Existing Session");
        result.Stages[0].MissionId.Should().Be(session.Stages[0].MissionId);
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

        var session = Session.Create("Pin Session", "777777", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
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

        var session = Session.Create("Named Session", "888888", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
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
            Session.Create("Session A", "111111", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") }),
            Session.Create("Session B", "222222", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") }),
            Session.Create("Session C", "333333", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") }),
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
        dbContext.Sessions.Add(Session.Create("Alpha Session", "444444", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") }));
        dbContext.Sessions.Add(Session.Create("Beta Session", "555555", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") }));
        dbContext.Sessions.Add(Session.Create("Alpha Beta Session", "666666", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") }));
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
        dbContext.Sessions.Add(Session.Create("Session 1", "111111", new List<SessionStage> { SessionStage.Create(missionId1, Guid.NewGuid(), "Mission 1", "Stage", "Trivia", 1, "test-token") }));
        dbContext.Sessions.Add(Session.Create("Session 2", "222222", new List<SessionStage> { SessionStage.Create(missionId2, Guid.NewGuid(), "Mission 2", "Stage", "Trivia", 1, "test-token") }));
        dbContext.Sessions.Add(Session.Create("Session 3", "333333", new List<SessionStage> { SessionStage.Create(missionId1, Guid.NewGuid(), "Mission 1", "Stage", "Trivia", 1, "test-token") }));
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetSessionsAsync(null, null, missionId1, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(2);
        result.Should().HaveCount(2);
        result.All(s => s.Stages.Any(st => st.MissionId == missionId1)).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistSessionChanges()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Original Name", "999999", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Original Mission", "Stage", "Trivia", 1, "test-token") });
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

        var session = Session.Create("Participant Session", "000001", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
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

    [Fact]
    public async Task ApplyCluePenaltyAsync_WithReason_ShouldPersistAuditEventWithReasonAndNegativeScoreDelta()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Penalty Session", "000002", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Treasure", 1, "test-token") });
        dbContext.Sessions.Add(session);

        var team = SessionTeam.Create(session.Id, "Team A");
        dbContext.SessionTeams.Add(team);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        await repo.ApplyCluePenaltyAsync(session.Id, team.Id, 25, "Pista revelada antes de tiempo", CancellationToken.None);

        // Assert
        var events = await dbContext.Set<SessionAuditEvent>()
            .Where(e => e.SessionId == session.Id)
            .ToListAsync();

        events.Should().ContainSingle();
        var evt = events[0];
        evt.EventType.Should().Be(SessionAuditEventTypes.PenaltyApplied);
        evt.Description.Should().Be("Pista revelada antes de tiempo");
        evt.TeamId.Should().Be(team.Id);
        evt.ScoreDelta.Should().Be(-25);
    }

    [Fact]
    public async Task ApplyCluePenaltyAsync_WithoutReason_ShouldPersistAuditEventWithDefaultDescription()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Penalty Session 2", "000003", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Treasure", 1, "test-token") });
        dbContext.Sessions.Add(session);

        var team = SessionTeam.Create(session.Id, "Team B");
        dbContext.SessionTeams.Add(team);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        await repo.ApplyCluePenaltyAsync(session.Id, team.Id, 10, ct: CancellationToken.None);

        // Assert
        var evt = await dbContext.Set<SessionAuditEvent>().FirstOrDefaultAsync(e => e.SessionId == session.Id);
        evt.Should().NotBeNull();
        evt!.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAuditTrailAsync_ShouldReturnEventsForSessionOrderedByMostRecentFirst()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var sessionId = Guid.NewGuid();
        var otherSessionId = Guid.NewGuid();

        var older = SessionAuditEvent.Create(sessionId, SessionAuditEventTypes.StatusChanged, "Sesión transicionó de 'Scheduled' a 'Preparing'");
        await Task.Delay(5);
        var newer = SessionAuditEvent.Create(sessionId, SessionAuditEventTypes.StatusChanged, "Sesión transicionó de 'Preparing' a 'Active'");
        var unrelated = SessionAuditEvent.Create(otherSessionId, SessionAuditEventTypes.StatusChanged, "Otra sesión");

        dbContext.Set<SessionAuditEvent>().AddRange(older, newer, unrelated);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetAuditTrailAsync(sessionId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(e => e.Id).Should().NotContain(unrelated.Id);
        result[0].Id.Should().Be(newer.Id);
        result[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task GetSessionRankingAsync_WhenTeamsAreTied_ShouldBreakTieByWhoScoredFirst()
    {
        // Arrange — RB-08: ranking ordered by score descending, time as tie-break.
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var session = Session.Create("Ranking Session", "000004", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Treasure", 1, "test-token") });
        dbContext.Sessions.Add(session);

        var firstTeam = SessionTeam.Create(session.Id, "First To Score");
        var secondTeam = SessionTeam.Create(session.Id, "Second To Score");
        dbContext.SessionTeams.AddRange(firstTeam, secondTeam);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);
        await repo.AddTeamScoreAsync(firstTeam.Id, 100, CancellationToken.None);
        await Task.Delay(15);
        await repo.AddTeamScoreAsync(secondTeam.Id, 100, CancellationToken.None);

        // Act
        var ranking = await repo.GetSessionRankingAsync(session.Id, CancellationToken.None);

        // Assert
        ranking.Should().HaveCount(2);
        ranking[0].Score.Should().Be(ranking[1].Score);
        ranking[0].TeamId.Should().Be(firstTeam.Id);
        ranking[1].TeamId.Should().Be(secondTeam.Id);
    }

    [Fact]
    public async Task GetSessionsForParticipantAsync_ShouldReturnOnlySessionsTheUserJoined()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var userId = Guid.NewGuid();

        var joinedSession = Session.Create("Joined", "555001", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
        joinedSession.TransitionTo(SessionStatus.Preparing);
        joinedSession.AddParticipant(userId);

        var otherSession = Session.Create("Not Joined", "555002", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
        otherSession.TransitionTo(SessionStatus.Preparing);
        otherSession.AddParticipant(Guid.NewGuid());

        dbContext.Sessions.AddRange(joinedSession, otherSession);
        await dbContext.SaveChangesAsync();

        var repo = new SessionRepository(dbContext);

        // Act
        var result = await repo.GetSessionsForParticipantAsync(userId, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(joinedSession.Id);
    }
}

