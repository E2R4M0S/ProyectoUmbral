using FluentAssertions;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.List;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.List;

public class GetTeamsQueryHandlerTests
{
    private readonly ITeamRepository _repository = Substitute.For<ITeamRepository>();
    private readonly GetTeamsQueryHandler _sut;

    public GetTeamsQueryHandlerTests()
    {
        _sut = new GetTeamsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsItemsAndTotalCount()
    {
        // Arrange
        var team1 = Team.Create("Los Leones", "Desc A", "leader-1");
        team1.AddMember("member-1");
        var team2 = Team.Create("Los Tigres", "Desc B", "leader-2");

        var teams = new List<Team> { team1, team2 };

        _repository.GetTeamsAsync(null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 2));

        var query = new GetTeamsQuery(null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_AppliesSearchFilter_CaseInsensitive()
    {
        // Arrange
        var team = Team.Create("Los Leones", "Desc", "leader-1");
        var teams = new List<Team> { team };

        _repository.GetTeamsAsync("leones", 1, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 1));

        var query = new GetTeamsQuery("leones", 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Los Leones");

        await _repository.Received(1).GetTeamsAsync(
            "leones", 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var teams = new List<Team>();
        for (int i = 0; i < 5; i++)
        {
            teams.Add(Team.Create($"Team {i}", $"Desc {i}", $"leader-{i}"));
        }

        _repository.GetTeamsAsync(null, 2, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 15));

        var query = new GetTeamsQuery(null, 2, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyListAndZeroTotalCount()
    {
        // Arrange
        _repository.GetTeamsAsync("nonexistent", 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Team>().AsReadOnly(), 0));

        var query = new GetTeamsQuery("nonexistent", 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithInvalidPage_DefaultsToPage1()
    {
        // Arrange
        var team = Team.Create("Team A", "Desc", "leader-1");
        var teams = new List<Team> { team };

        _repository.GetTeamsAsync(null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 1));

        var query = new GetTeamsQuery(null, -5, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Should().Be(1);

        await _repository.Received(1).GetTeamsAsync(
            null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPageSize_DefaultsTo10()
    {
        // Arrange
        var team = Team.Create("Team A", "Desc", "leader-1");
        var teams = new List<Team> { team };

        _repository.GetTeamsAsync(null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 1));

        var query = new GetTeamsQuery(null, 1, 0);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.PageSize.Should().Be(10);

        await _repository.Received(1).GetTeamsAsync(
            null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsCorrectMemberCount()
    {
        // Arrange
        var team = Team.Create("Los Leones", "Desc", "leader-1");
        team.AddMember("member-1");
        team.AddMember("member-2");
        team.AddMember("member-3");

        var teams = new List<Team> { team };

        _repository.GetTeamsAsync(null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 1));

        var query = new GetTeamsQuery(null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].MemberCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ReturnsJoinCode()
    {
        // Arrange
        var team = Team.Create("Los Leones", "Desc", "leader-1");
        team.GenerateJoinCode();

        var teams = new List<Team> { team };

        _repository.GetTeamsAsync(null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((teams.AsReadOnly(), 1));

        var query = new GetTeamsQuery(null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].JoinCode.Should().NotBeNullOrEmpty();
        result.Items[0].JoinCode.Should().HaveLength(6);
    }
}
