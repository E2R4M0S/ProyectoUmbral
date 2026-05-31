using MediatR;

namespace Trivia.Application.Questions.Commands;

public record EditQuestionCommand(Guid QuizId, Guid QuestionId, string Text, int TimeLimitSeconds, int Order) : IRequest<MediatR.Unit>;
