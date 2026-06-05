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

    [Fact]
    public async Task AddAsync_ShouldPersistMission()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var repo = new MissionRepository(dbContext);

        var mission = Mission.Create("New Mission", "Description", Difficulty.Easy, 15, MissionType.Treasure);

        // Act
        await repo.AddAsync(mission, CancellationToken.None);

        // Assert
        var saved = await dbContext.Missions.FirstOrDefaultAsync(m => m.Id == mission.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("New Mission");
        saved.Status.Should().Be(MissionStatus.Draft);
    }

    [Fact]
    public async Task IsTitleUniqueAsync_WhenTitleIsUnique_ReturnsTrue()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var existing = Mission.Create("Existing Mission", "Desc", Difficulty.Easy, 10, MissionType.Treasure);
        dbContext.Missions.Add(existing);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var result = await repo.IsTitleUniqueAsync("Different Title", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTitleUniqueAsync_WhenTitleExists_ReturnsFalse()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var existing = Mission.Create("Unique Title", "Desc", Difficulty.Easy, 10, MissionType.Treasure);
        dbContext.Missions.Add(existing);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var result = await repo.IsTitleUniqueAsync("Unique Title", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTitleUniqueAsync_WhenDuplicateButExcludedId_ReturnsTrue()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var existing = Mission.Create("Same Title", "Desc", Difficulty.Medium, 20, MissionType.Trivia);
        dbContext.Missions.Add(existing);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        var result = await repo.IsTitleUniqueAsync("Same Title", CancellationToken.None, existing.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateMissionProperties()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var mission = Mission.Create("Original Title", "Original Desc", Difficulty.Easy, 10, MissionType.Treasure);
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        mission.Update("Updated Title", "Updated Desc", Difficulty.Hard, 30);
        await repo.UpdateAsync(mission, CancellationToken.None);

        // Assert
        var saved = await dbContext.Missions.FirstOrDefaultAsync(m => m.Id == mission.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Updated Title");
        saved.Description.Should().Be("Updated Desc");
        saved.Difficulty.Should().Be(Difficulty.Hard);
        saved.TimeMinutes.Should().Be(30);
    }

    [Fact]
    public async Task AddStageAsync_ShouldAddStageToMission()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var mission = Mission.Create("Stage Test Mission", "Desc", Difficulty.Medium, 20, MissionType.Treasure);
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);
        mission.AddStage("Stage 1", "Stage Description", 1);

        // Act
        await repo.AddStageAsync(mission, CancellationToken.None);

        // Assert
        var saved = await dbContext.Missions
            .Include(m => m.Stages)
            .FirstOrDefaultAsync(m => m.Id == mission.Id);
        saved.Should().NotBeNull();
        saved!.Stages.Should().HaveCount(1);
        saved.Stages.First().Name.Should().Be("Stage 1");
    }

    [Fact]
    public async Task AddClueAsync_ShouldAddClueToStage()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var mission = Mission.Create("Clue Test Mission", "Desc", Difficulty.Medium, 20, MissionType.Treasure);
        mission.AddStage("Stage 1", "Stage Desc", 1);
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        var stageId = mission.Stages.First().Id;
        var repo = new MissionRepository(dbContext);

        // Add a clue to the stage via mission aggregate
        mission.AddStageClue(stageId, "First Clue", 10, ReleaseType.Manual);

        // Act
        await repo.AddClueAsync(mission, stageId, CancellationToken.None);

        // Assert
        var saved = await dbContext.Missions
            .Include(m => m.Stages)
                .ThenInclude(s => s.Clues)
            .FirstOrDefaultAsync(m => m.Id == mission.Id);
        saved.Should().NotBeNull();
        saved!.Stages.First().Clues.Should().HaveCount(1);
        saved.Stages.First().Clues.First().Content.Should().Be("First Clue");
    }

    [Fact]
    public async Task RemoveStage_ShouldRemoveStageFromDatabase()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var mission = Mission.Create("Remove Stage Mission", "Desc", Difficulty.Easy, 15, MissionType.Treasure);
        mission.AddStage("Stage To Remove", "Desc", 1);
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        var stage = mission.Stages.First();
        var repo = new MissionRepository(dbContext);

        // Act
        repo.RemoveStage(mission, stage);
        await dbContext.SaveChangesAsync();

        // Assert
        var saved = await dbContext.Missions
            .Include(m => m.Stages)
            .FirstOrDefaultAsync(m => m.Id == mission.Id);
        saved.Should().NotBeNull();
        saved!.Stages.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);

        var mission = Mission.Create("Save Changes Mission", "Desc", Difficulty.Easy, 10, MissionType.Trivia);
        dbContext.Missions.Add(mission);
        await dbContext.SaveChangesAsync();

        var repo = new MissionRepository(dbContext);

        // Act
        mission.Update("Updated Title", "Updated Desc", Difficulty.Medium, 25);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Assert
        var saved = await dbContext.Missions.FirstOrDefaultAsync(m => m.Id == mission.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Updated Title");
        saved.TimeMinutes.Should().Be(25);
    }
}