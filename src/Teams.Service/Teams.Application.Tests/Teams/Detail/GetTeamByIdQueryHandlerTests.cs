using FluentAssertions;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Detail;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.Detail;

public class GetTeamByIdQueryHandlerTests
{
    private readonly ITeamRepository _repository = Substitute.For<ITeamRepository>();
    private readonly GetTeamByIdQueryHandler _sut;

    public GetTeamByIdQueryHandlerTests()
    {
        _sut = new GetTeamByIdQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenTeamExists_ReturnsTeamDetailDto()
    {
        // Arrange
        var team = Team.Create("Los Leones", "Best team", "leader-1");
        team.AddMember("member-1");
        team.AddMember("member-2");
        team.GenerateJoinCode();

        _repository.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>())
            .Returns(team);

        var query = new GetTeamByIdQuery(team.Id);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(team.Id);
        result.Name.Should().Be("Los Leones");
        result.Description.Should().Be("Best team");
        result.LeaderId.Should().Be("leader-1");
        result.JoinCode.Should().NotBeNullOrEmpty();
        result.JoinCode.Should().HaveLength(6);
        result.Members.Should().HaveCount(2);
        result.Members[0].UserId.Should().Be("member-1");
        result.Members[1].UserId.Should().Be("member-2");
    }

    [Fact]
    public async Task Handle_WhenTeamDoesNotExist_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository.GetByIdWithMembersAsync(id, Arg.Any<CancellationToken>())
            .Returns((Team?)null);

        var query = new GetTeamByIdQuery(id);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}