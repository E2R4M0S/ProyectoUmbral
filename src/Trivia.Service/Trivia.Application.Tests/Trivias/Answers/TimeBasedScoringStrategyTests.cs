using System;
using FluentAssertions;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Common.Strategies;
using Xunit;

namespace Trivia.Application.Tests.Trivias.Answers;

public class TimeBasedScoringStrategyTests
{
    private readonly IScoringStrategy _strategy = new TimeBasedScoringStrategy();

    [Fact]
    public void CalculateScore_FirstQuarter_Returns40()
    {
        // First 25% of time (e.g., 7.5s out of 30s limit)
        var timeElapsed = TimeSpan.FromSeconds(7.5);

        var result = _strategy.CalculateScore(timeElapsed, 30);

        result.Should().Be(40);
    }

    [Fact]
    public void CalculateScore_FirstHalf_Returns30()
    {
        // Between 25% and 50% (e.g., 10s out of 30s limit)
        var timeElapsed = TimeSpan.FromSeconds(10);

        var result = _strategy.CalculateScore(timeElapsed, 30);

        result.Should().Be(30);
    }

    [Fact]
    public void CalculateScore_ThirdQuarter_Returns20()
    {
        // Between 50% and 75% (e.g., 20s out of 30s limit)
        var timeElapsed = TimeSpan.FromSeconds(20);

        var result = _strategy.CalculateScore(timeElapsed, 30);

        result.Should().Be(20);
    }

    [Fact]
    public void CalculateScore_LastQuarter_Returns10()
    {
        // Between 75% and 100% (e.g., 28s out of 30s limit)
        var timeElapsed = TimeSpan.FromSeconds(28);

        var result = _strategy.CalculateScore(timeElapsed, 30);

        result.Should().Be(10);
    }

}