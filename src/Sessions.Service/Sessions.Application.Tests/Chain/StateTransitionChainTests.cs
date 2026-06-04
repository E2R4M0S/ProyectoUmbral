using FluentAssertions;
using Sessions.Application.Sessions.Transition.Chain;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Sessions.Application.Tests.Chain;

public class ValidStatusHandlerTests
{
    [Fact]
    public void ValidStatus_ShouldPass()
    {
        var handler = new ValidStatusHandler();
        var session = Session.Create("test", Guid.NewGuid(), "Mission", "123456");
        var act = () => handler.Handle(session, "Active");
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidStatus_ShouldThrow()
    {
        var handler = new ValidStatusHandler();
        var session = Session.Create("test", Guid.NewGuid(), "Mission", "123456");
        var act = () => handler.Handle(session, "Invisible");
        act.Should().Throw<InvalidOperationException>();
    }
}

public class NotTerminalHandlerTests
{
    [Fact]
    public void FinishedSession_ShouldThrow()
    {
        var handler = new NotTerminalHandler();
        var session = Session.Create("test", Guid.NewGuid(), "Mission", "123456");
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        session.TransitionTo(SessionStatus.Finished);
        var act = () => handler.Handle(session, "Active");
        act.Should().Throw<InvalidOperationException>();
    }
}

public class ChainIntegrationTests
{
    [Fact]
    public void FullChain_ValidTransition_ShouldPass()
    {
        var validStatus = new ValidStatusHandler();
        var notTerminal = new NotTerminalHandler();
        validStatus.SetNext(notTerminal);

        var session = Session.Create("test", Guid.NewGuid(), "Mission", "123456");
        var act = () => validStatus.Handle(session, "Active");
        act.Should().NotThrow();
    }
}
