using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Common.Strategies;

public class TimeBasedScoringStrategy : IScoringStrategy
{
    public int CalculateScore(TimeSpan timeElapsed, int timeLimitSeconds)
    {
        if (timeLimitSeconds <= 0) return 10;

        var elapsedSec = timeElapsed.TotalSeconds;
        var ratio = elapsedSec / timeLimitSeconds;

        return ratio switch
        {
            <= 0.25 => 40, // responded in first 25% of time
            <= 0.50 => 30, // responded in first half
            <= 0.75 => 20, // responded in first 3/4
            _       => 10  // responded in last quarter or overtime
        };
    }
}
