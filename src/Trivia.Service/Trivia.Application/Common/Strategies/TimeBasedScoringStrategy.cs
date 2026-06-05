using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Common.Strategies;

public class TimeBasedScoringStrategy : IScoringStrategy
{
    public int CalculateScore(TimeSpan timeElapsed, int timeLimitSeconds)
    {
        var elapsedSec = timeElapsed.TotalSeconds;
        var ratio = elapsedSec / timeLimitSeconds;
        return ratio switch
        {
            <= 0.25 => 40,  // first 25% of time
            <= 0.50 => 30,  // first half
            <= 0.75 => 20,  // first 3/4
            _       => 10   // last quarter or overtime
        };
    }
}