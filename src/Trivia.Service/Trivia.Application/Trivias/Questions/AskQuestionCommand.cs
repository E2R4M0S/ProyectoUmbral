using MediatR;

namespace Trivia.Application.Trivias.Questions;

public record AskQuestionCommand(
    Guid SessionId,
    string QuestionText,
    string[] Options,
    int TimeLimitSeconds,
    int CorrectAnswerIndex,
    // Persisted quiz-bank question this live round comes from, if any (HU-27/HU-29:
    // lets us mark ReleasedAt and reuse the same id UserAnswers/TeamAnswers key on).
    Guid? QuestionId = null
) : IRequest<Guid>;
