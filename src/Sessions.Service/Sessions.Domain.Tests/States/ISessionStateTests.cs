using FluentAssertions;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Domain.Tests.States;

public class ISessionStateTests
{
    [Fact]
    public void Interface_ShouldDeclareStatusProperty()
    {
        // Verify the interface contract
        var type = typeof(Domain.States.ISessionState);
        var statusProperty = type.GetProperty("Status");

        statusProperty.Should().NotBeNull();
        statusProperty!.PropertyType.Should().Be(typeof(SessionStatus));
    }

    [Fact]
    public void Interface_ShouldDeclareCanTransitionTo()
    {
        var type = typeof(Domain.States.ISessionState);
        var method = type.GetMethod("CanTransitionTo");

        method.Should().NotBeNull();
        method!.GetParameters()[0].ParameterType.Should().Be(typeof(SessionStatus));
    }

    [Fact]
    public void Interface_ShouldDeclareOnEnterAndOnExit()
    {
        var type = typeof(Domain.States.ISessionState);
        var onEnter = type.GetMethod("OnEnter");
        var onExit = type.GetMethod("OnExit");

        onEnter.Should().NotBeNull();
        onExit.Should().NotBeNull();
        onEnter!.GetParameters()[0].ParameterType.Name.Should().Contain("Session");
        onExit!.GetParameters()[0].ParameterType.Name.Should().Contain("Session");
    }
}