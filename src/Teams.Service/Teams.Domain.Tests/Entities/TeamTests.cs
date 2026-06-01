using FluentAssertions;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Domain.Tests.Entities;

public class TeamTests
{
    // ===== Create Tests =====

    [Fact]
    public void Create_ShouldCreateTeam()
    {
        var team = Team.Create("Los Leones", "Equipo de desarrollo", "leader-123");

        team.Name.Should().Be("Los Leones");
        team.Description.Should().Be("Equipo de desarrollo");
        team.LeaderId.Should().Be("leader-123");
        team.Id.Should().NotBeEmpty();
        team.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_EmptyMembers_ShouldInitializeEmptyList()
    {
        var team = Team.Create("Los Tigres", "Equipo QA", "leader-456");

        team.Members.Should().NotBeNull();
        team.Members.Should().BeEmpty();
    }

    [Fact]
    public void Create_EmptyName_ShouldAssignEmpty()
    {
        var team = Team.Create("", "Some description", "leader-123");

        team.Name.Should().BeEmpty();
    }

    [Fact]
    public void Name_ShouldBeTrimmed()
    {
        var team = Team.Create("  Los Lobos  ", "Equipo móvil", "leader-333");

        team.Name.Should().Be("Los Lobos");
    }

    // ===== AddMember Tests =====

    [Fact]
    public void AddMember_ShouldAddMemberToTeam()
    {
        var team = Team.Create("Los Halcones", "Equipo frontend", "leader-789");
        var userId = "user-member-1";

        team.AddMember(userId);

        team.Members.Should().HaveCount(1);
        team.Members[0].UserId.Should().Be(userId);
    }

    [Fact]
    public void AddMember_ShouldSetCorrectTeamId()
    {
        var team = Team.Create("Los Búhos", "Equipo backend", "leader-111");
        var userId = "user-member-2";

        team.AddMember(userId);

        team.Members[0].TeamId.Should().Be(team.Id);
    }

    [Fact]
    public void AddMember_ShouldGenerateUniqueIds()
    {
        var team = Team.Create("Los Águilas", "Equipo fullstack", "leader-222");
        var userId1 = "user-1";
        var userId2 = "user-2";

        team.AddMember(userId1);
        team.AddMember(userId2);

        team.Members[0].Id.Should().NotBe(team.Members[1].Id);
    }

    [Fact]
    public void AddMember_Duplicate_ShouldThrow()
    {
        var team = Team.Create("Los Cóndores", "Equipo ops", "leader-555");
        var userId = "user-dup";

        team.AddMember(userId);

        Action act = () => team.AddMember(userId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already a member*");
    }

    // ===== RemoveMember Tests =====

    [Fact]
    public void RemoveMember_ShouldRemoveMember()
    {
        var team = Team.Create("Los Pumas", "Equipo security", "leader-666");
        var userId = "user-remove";

        team.AddMember(userId);
        team.RemoveMember(userId);

        team.Members.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMember_NotFound_ShouldThrow()
    {
        var team = Team.Create("Los Leopardos", "Equipo data", "leader-777");

        Action act = () => team.RemoveMember("non-member");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a member*");
    }

    [Fact]
    public void RemoveMember_ShouldReduceCount()
    {
        var team = Team.Create("Los Jaguares", "Equipo ml", "leader-888");
        team.AddMember("user-1");
        team.AddMember("user-2");

        team.RemoveMember("user-1");

        team.Members.Should().HaveCount(1);
    }

    // ===== Update Tests =====

    [Fact]
    public void Update_NameAndDescription_ShouldAssignNewValues()
    {
        var team = Team.Create("Los Zorros", "Viejo equipo", "leader-999");

        team.Update("Los Zorros Veloces", "Equipo renombrado");

        team.Name.Should().Be("Los Zorros Veloces");
        team.Description.Should().Be("Equipo renombrado");
    }

    [Fact]
    public void Update_ShouldTrimName()
    {
        var team = Team.Create("Los Lobos", "Descripción", "leader-000");

        team.Update("  Nuevos Lobos  ", "Nueva descripción");

        team.Name.Should().Be("Nuevos Lobos");
    }

    [Fact]
    public void Update_LeaderId_ShouldRemainUnchanged()
    {
        var team = Team.Create("Los Gatos", "Equipo felino", "original-leader");

        team.Update("Nuevos Gatos", "Descripción actualizada");

        team.LeaderId.Should().Be("original-leader");
    }

    // ===== JoinCode Tests =====

    [Fact]
    public void JoinCode_ShouldBeNullByDefault()
    {
        var team = Team.Create("Los Toros", "Equipo fuerte", "leader-111");

        team.JoinCode.Should().BeNull();
    }

    [Fact]
    public void GenerateJoinCode_ShouldReturn6CharAlphanumeric()
    {
        var team = Team.Create("Los Leones", "Equipo de desarrollo", "leader-123");

        team.GenerateJoinCode();

        team.JoinCode.Should().NotBeNullOrEmpty();
        team.JoinCode.Should().HaveLength(6);
        team.JoinCode.Should().MatchRegex("^[A-Z0-9]{6}$");
    }

    [Fact]
    public void GenerateJoinCode_ShouldGenerateUniqueCodes()
    {
        var team1 = Team.Create("Los Leones", "Desc 1", "leader-1");
        var team2 = Team.Create("Los Tigres", "Desc 2", "leader-2");
        var team3 = Team.Create("Los Búhos", "Desc 3", "leader-3");

        team1.GenerateJoinCode();
        team2.GenerateJoinCode();
        team3.GenerateJoinCode();

        team1.JoinCode.Should().NotBe(team2.JoinCode);
        team1.JoinCode.Should().NotBe(team3.JoinCode);
        team2.JoinCode.Should().NotBe(team3.JoinCode);
    }

    [Fact]
    public void GenerateJoinCode_MultipleCalls_ShouldProduceNewCode()
    {
        var team = Team.Create("Los Halcones", "Desc", "leader-1");
        team.GenerateJoinCode();
        var firstCode = team.JoinCode;

        team.GenerateJoinCode();

        team.JoinCode.Should().NotBe(firstCode);
    }
}
