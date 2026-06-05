namespace Trivia.Application.Common.Interfaces;

public interface IScoringStrategy
{
    int CalculateScore(TimeSpan timeElapsed, int timeLimitSeconds);
}