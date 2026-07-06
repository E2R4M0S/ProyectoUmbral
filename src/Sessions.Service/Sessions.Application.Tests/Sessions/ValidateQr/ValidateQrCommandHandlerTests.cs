using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.ValidateQr;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.ValidateQr;

public class ValidateQrCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly ILogger<ValidateQrCommandHandler> _logger =
        Substitute.For<ILogger<ValidateQrCommandHandler>>();
    private readonly ValidateQrCommandHandler _sut;

    public ValidateQrCommandHandlerTests()
    {
        _sut = new ValidateQrCommandHandler(_repository, _facade, _logger);
    }

    private static Session BuildActiveSession(IReadOnlyList<SessionStage> stages)
    {
        var session = Session.Create("Test Session", "111222", stages.ToList());
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        return session;
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnInvalidResult()
    {
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns((Session?)null);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), Guid.NewGuid(), "token"), default);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_SessionNotActive_ShouldReturnInvalidResult()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Treasure", 1, "tok");
        var session = Session.Create("Test", "123456", new List<SessionStage> { stage });
        // Session is still Scheduled (not Active)
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), stageId, "tok"), default);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public async Task Handle_InvalidQrToken_ShouldReturnInvalidResult()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Treasure", 1, "correct-token");
        var session = BuildActiveSession(new[] { stage });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), stageId, "wrong-token"), default);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid QR");
    }

    [Fact]
    public async Task Handle_ValidQrOnLastStage_ShouldReturnLastStageWithoutAdvancing()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Treasure", 1, "tok");
        var session = BuildActiveSession(new[] { stage });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), stageId, "tok"), default);

        result.IsValid.Should().BeTrue();
        result.Advanced.Should().BeFalse();
        result.IsLastStage.Should().BeTrue();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrNotLastStage_ShouldAdvanceAndNotifyFacade()
    {
        var stage1Id = Guid.NewGuid();
        var stage1 = SessionStage.Create(Guid.NewGuid(), stage1Id, "M1", "Treasure", 1, "tok1");
        var stage2 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Treasure", 2, "tok2");
        var stage3 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M3", "Treasure", 3, "tok3");
        var session = BuildActiveSession(new[] { stage1, stage2, stage3 });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), stage1Id, "tok1"), default);

        result.IsValid.Should().BeTrue();
        result.Advanced.Should().BeTrue();
        result.IsLastStage.Should().BeFalse();
        await _repository.Received(1).UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _facade.Received(1).NotifyStageAdvanced(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrNotLastStage_ShouldReturnUpdatedStageOrder()
    {
        var stage1Id = Guid.NewGuid();
        var stage1 = SessionStage.Create(Guid.NewGuid(), stage1Id, "M1", "Treasure", 1, "tok1");
        var stage2 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Treasure", 2, "tok2");
        var stage3 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M3", "Treasure", 3, "tok3");
        var session = BuildActiveSession(new[] { stage1, stage2, stage3 });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), stage1Id, "tok1"), default);

        result.CurrentStageOrder.Should().Be(1);
        result.TotalStages.Should().Be(3);
    }
}
