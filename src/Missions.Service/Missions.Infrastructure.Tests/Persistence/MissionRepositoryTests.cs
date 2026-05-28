using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Missions.Infrastructure.Persistence;
using Xunit;

namespace Missions.Infrastructure.Tests.Persistence;

public class MissionRepositoryTests
{
    private static MissionsDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MissionsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new MissionsDbContext(options);
    }

    [Fact]
    public async Task GetMissionsAsync_WithNoFilters_ReturnsAllMissionsPaginated()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var missions = new[]
        {
            Mission.Create("Mission A", "Desc A", Difficulty.Easy, 10, MissionType.Treasure),
            Mission.Create("Mission B", "Desc B", Difficulty.Medium, 20, MissionType.Trivia),
            Mission.Create("Mission C", "Desc C", Difficulty.Hard, 30, MissionType.Treasure),
        };

        // Set CreatedAt using reflection since InMemory doesn't auto-set it
        int i = 0;
        foreach (var m in missions)
        {
            typeof(Mission).GetProperty("CreatedAt")!.SetValue(m, DateTime.UtcNow.AddMinutes(i++));
            dbContext.Missions.Add(m);
        }
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetMissionsAsync(null, null, null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(3);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMissionsAsync_WithSearchFilter_ReturnsMatchingMissions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var m1 = Mission.Create("Find Treasure", "Desc", Difficulty.Easy, 10, MissionType.Treasure);
        var m2 = Mission.Create("Trivia Night", "Desc", Difficulty.Medium, 20, MissionType.Trivia);
        var m3 = Mission.Create("Hidden Treasure Hunt", "Desc", Difficulty.Hard, 30, MissionType.Treasure);

        foreach (var m in new[] { m1, m2, m3 })
        {
            dbContext.Missions.Add(m);
        }
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetMissionsAsync("treasure", null, null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(2);
        result.Should().HaveCount(2);
        result.All(m => m.Title.Contains("Treasure", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task GetMissionsAsync_WithDifficultyFilter_ReturnsMatchingMissions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var m1 = Mission.Create("Easy Mission", "Desc", Difficulty.Easy, 10, MissionType.Treasure);
        var m2 = Mission.Create("Medium Mission", "Desc", Difficulty.Medium, 20, MissionType.Trivia);
        var m3 = Mission.Create("Hard Mission", "Desc", Difficulty.Hard, 30, MissionType.Treasure);

        foreach (var m in new[] { m1, m2, m3 })
        {
            dbContext.Missions.Add(m);
        }
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetMissionsAsync(null, "Hard", null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        result.Should().HaveCount(1);
        result[0].Difficulty.Should().Be(Difficulty.Hard);
    }

    [Fact]
    public async Task GetMissionsAsync_WithStatusFilter_ReturnsMatchingMissions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var m1 = Mission.Create("Draft Mission", "Desc", Difficulty.Easy, 10, MissionType.Treasure);
        var m2 = Mission.Create("Active Mission", "Desc", Difficulty.Medium, 20, MissionType.Trivia);

        // Set m2 to Active status using reflection
        typeof(Mission).GetProperty("Status")!.SetValue(m2, MissionStatus.Active);

        foreach (var m in new[] { m1, m2 })
        {
            dbContext.Missions.Add(m);
        }
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetMissionsAsync(null, null, "Active", 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(1);
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(MissionStatus.Active);
    }

    [Fact]
    public async Task GetMissionsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        for (int i = 0; i < 15; i++)
        {
            var m = Mission.Create($"Mission {i}", $"Desc {i}", Difficulty.Easy, 10, MissionType.Treasure);
            dbContext.Missions.Add(m);
        }
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act — page 2, pageSize 10
        var (result, totalCount) = await repo.GetMissionsAsync(null, null, null, 2, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(15);
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetMissionsAsync_WithEmptyResult_ReturnsEmptyListAndZeroCount()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new MissionRepository(dbContext);

        // Act
        var (result, totalCount) = await repo.GetMissionsAsync("nonexistent", null, null, 1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(0);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissionExists_ReturnsMission()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var m = Mission.Create("Find Treasure", "Secret location", Difficulty.Hard, 45, MissionType.Treasure);
        dbContext.Missions.Add(m);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);
        var id = m.Id;

        // Act
        var result = await repo.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Find Treasure");
        result.Description.Should().Be("Secret location");
        result.Difficulty.Should().Be(Difficulty.Hard);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new MissionRepository(dbContext);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}