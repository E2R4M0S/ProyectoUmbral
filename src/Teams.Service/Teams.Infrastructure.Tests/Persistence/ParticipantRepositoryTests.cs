using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Teams.Domain.Entities;
using Teams.Infrastructure;
using Teams.Infrastructure.Persistence;
using Xunit;

namespace Teams.Infrastructure.Tests.Persistence;

public class ParticipantRepositoryTests
{
    private static TeamsDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TeamsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new TeamsDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistParticipant()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new ParticipantRepository(dbContext);
        var participant = Participant.Create("Test", "User", "testuser", "testalias", "test@test.com", "kc-123");

        await repo.AddAsync(participant, CancellationToken.None);

        var saved = await dbContext.Participants.FirstOrDefaultAsync(p => p.Id == participant.Id);
        saved.Should().NotBeNull();
        saved!.FirstName.Should().Be("Test");
        saved.LastName.Should().Be("User");
        saved.Alias.Should().Be("testalias");
        saved.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task IsAliasUniqueAsync_WhenAliasIsFree_ReturnsTrue()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new ParticipantRepository(dbContext);

        var result = await repo.IsAliasUniqueAsync("uniquealias", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAliasUniqueAsync_WhenAliasExists_ReturnsFalse()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var participant = Participant.Create("User", "Name", "username", "taken", "u@test.com", "kc-1");
        dbContext.Participants.Add(participant);
        await dbContext.SaveChangesAsync();

        var repo = new ParticipantRepository(dbContext);
        var result = await repo.IsAliasUniqueAsync("taken", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailIsFree_ReturnsTrue()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new ParticipantRepository(dbContext);

        var result = await repo.IsEmailUniqueAsync("free@test.com", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailExists_ReturnsFalse()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var participant = Participant.Create("User", "Name", "username", "alias", "taken@test.com", "kc-1");
        dbContext.Participants.Add(participant);
        await dbContext.SaveChangesAsync();

        var repo = new ParticipantRepository(dbContext);
        var result = await repo.IsEmailUniqueAsync("taken@test.com", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByKeycloakUserIdAsync_WhenExists_ReturnsParticipant()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var participant = Participant.Create("User", "Name", "username", "alias", "u@test.com", "kc-find-me");
        dbContext.Participants.Add(participant);
        await dbContext.SaveChangesAsync();

        var repo = new ParticipantRepository(dbContext);
        var result = await repo.GetByKeycloakUserIdAsync("kc-find-me", CancellationToken.None);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("User");
        result.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task GetByKeycloakUserIdAsync_WhenNotExists_ReturnsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new ParticipantRepository(dbContext);

        var result = await repo.GetByKeycloakUserIdAsync("nonexistent", CancellationToken.None);

        result.Should().BeNull();
    }
}
