using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Teams.Domain.Entities;
using Teams.Infrastructure;
using Teams.Infrastructure.Persistence;
using Xunit;

namespace Teams.Infrastructure.Tests.Persistence;

public class TeamRepositoryTests
{
    private static TeamsDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TeamsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new TeamsDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTeam()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new TeamRepository(dbContext);
        var team = Team.Create("Test Team", "A test team", "leader-123");

        await repo.AddAsync(team, CancellationToken.None);

        var saved = await dbContext.Teams.FirstOrDefaultAsync(t => t.Id == team.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Team");
        saved.Description.Should().Be("A test team");
    }

    [Fact]
    public async Task IsNameUniqueAsync_WhenNameIsFree_ReturnsTrue()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new TeamRepository(dbContext);

        var result = await repo.IsNameUniqueAsync("Nonexistent", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsNameUniqueAsync_WhenNameExists_ReturnsFalse()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var team = Team.Create("Existing Team", "desc", "leader-1");
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var repo = new TeamRepository(dbContext);
        var result = await repo.IsNameUniqueAsync("Existing Team", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByJoinCodeAsync_WhenCodeMatches_ReturnsTeam()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var team = Team.Create("Code Team", "desc", "leader-1");
        team.GenerateJoinCode();
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var repo = new TeamRepository(dbContext);
        var result = await repo.GetByJoinCodeAsync(team.JoinCode, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Code Team");
    }

    [Fact]
    public async Task GetByJoinCodeAsync_WhenCodeMissing_ReturnsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new TeamRepository(dbContext);

        var result = await repo.GetByJoinCodeAsync("ZZZZZZ", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearch_ReturnsFiltered()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        dbContext.Teams.Add(Team.Create("Alpha Team", "desc", "l1"));
        dbContext.Teams.Add(Team.Create("Beta Squad", "desc", "l2"));
        await dbContext.SaveChangesAsync();

        var repo = new TeamRepository(dbContext);
        var (teams, totalCount) = await repo.GetTeamsAsync("alpha", 1, 10, CancellationToken.None);

        teams.Should().HaveCount(1);
        teams[0].Name.Should().Be("Alpha Team");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTeamsAsync_Pagination_ReturnsCorrectPage()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        for (int i = 0; i < 5; i++)
            dbContext.Teams.Add(Team.Create($"Team {i}", "desc", $"leader-{i}"));
        await dbContext.SaveChangesAsync();

        var repo = new TeamRepository(dbContext);
        var (teams, totalCount) = await repo.GetTeamsAsync(null, 1, 3, CancellationToken.None);

        teams.Should().HaveCount(3);
        totalCount.Should().Be(5);
    }
}
