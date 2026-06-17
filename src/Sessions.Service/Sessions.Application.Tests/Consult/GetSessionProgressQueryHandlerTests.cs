using FluentAssertions;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Consult;

public class GetSessionProgressQueryHandlerTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly GetSessionProgressQueryHandler _sut;

    public GetSessionProgressQueryHandlerTests()
    {
        _sut = new GetSessionProgressQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Session?)null);
        var result = await _sut.Handle(new GetSessionProgressQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SessionExists_ShouldReturnProgress()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Mission", "Trivia", 1) });
        _repo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.Handle(new GetSessionProgressQuery(session.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Status.Should().Be(SessionStatus.Scheduled.ToString());
    }
}
