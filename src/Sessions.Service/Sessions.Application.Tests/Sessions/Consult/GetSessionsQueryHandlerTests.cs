using FluentAssertions;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Consult;

public class GetSessionsQueryHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly GetSessionsQueryHandler _sut;

    public GetSessionsQueryHandlerTests()
    {
        _sut = new GetSessionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsItemsAndTotalCount()
    {
        // Arrange
        var sessions = new List<Session>
        {
            Session.Create("Session A", "111111", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }),
            Session.Create("Session B", "222222", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }),
        };

        _repository.GetSessionsAsync(null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 2));

        var query = new GetSessionsQuery(null, null, null, 1, 10);

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
        var sessions = new List<Session>
        {
            Session.Create("Team Alpha Session", "333333", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }),
        };

        _repository.GetSessionsAsync("alpha", null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 1));

        var query = new GetSessionsQuery("alpha", null, null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Team Alpha Session");

        await _repository.Received(1).GetSessionsAsync(
            "alpha", null, null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppliesStatusFilter()
    {
        // Arrange
        var session = Session.Create("Active Session", "444444", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") });
        typeof(Session).GetProperty("Status")!.SetValue(session, SessionStatus.Active);

        var sessions = new List<Session> { session };

        _repository.GetSessionsAsync(null, "Active", null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 1));

        var query = new GetSessionsQuery(null, "Active", null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Active");

        await _repository.Received(1).GetSessionsAsync(
            null, "Active", null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppliesMissionIdFilter()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var sessions = new List<Session>
        {
            Session.Create("Mission Session", "555555", new List<SessionStage> { SessionStage.Create(missionId, Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }),
        };

        _repository.GetSessionsAsync(null, null, missionId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 1));

        var query = new GetSessionsQuery(null, null, missionId, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].MissionTitle.Should().Be("Test Mission");

        await _repository.Received(1).GetSessionsAsync(
            null, null, missionId, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var sessions = new List<Session>();
        for (int i = 0; i < 5; i++)
        {
            sessions.Add(Session.Create($"Session {i}", $"{100000 + i}", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }));
        }

        _repository.GetSessionsAsync(null, null, null, 2, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 15));

        var query = new GetSessionsQuery(null, null, null, 2, 10);

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
        _repository.GetSessionsAsync("nonexistent", null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Session>().AsReadOnly(), 0));

        var query = new GetSessionsQuery("nonexistent", null, null, 1, 10);

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
        var sessions = new List<Session>
        {
            Session.Create("Session A", "666666", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }),
        };

        _repository.GetSessionsAsync(null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 1));

        var query = new GetSessionsQuery(null, null, null, -5, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Should().Be(1);

        await _repository.Received(1).GetSessionsAsync(
            null, null, null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPageSize_DefaultsTo10()
    {
        // Arrange
        var sessions = new List<Session>
        {
            Session.Create("Session A", "777777", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, "test-token") }),
        };

        _repository.GetSessionsAsync(null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((sessions.AsReadOnly(), 1));

        var query = new GetSessionsQuery(null, null, null, 1, 0);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.PageSize.Should().Be(10);

        await _repository.Received(1).GetSessionsAsync(
            null, null, null, 1, 10, Arg.Any<CancellationToken>());
    }
}
