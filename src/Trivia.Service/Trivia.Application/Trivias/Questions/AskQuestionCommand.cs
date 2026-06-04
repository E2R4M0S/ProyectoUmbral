using MediatR;

namespace Trivia.Application.Trivias.Questions;

public record AskQuestionCommand(
    Guid SessionId,
    string QuestionText,
    string[] Options,
    int TimeLimitSeconds,
    int CorrectAnswerIndex
) : IRequest<Guid>;
