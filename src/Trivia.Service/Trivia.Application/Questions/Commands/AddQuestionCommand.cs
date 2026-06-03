using MediatR;

namespace Trivia.Application.Questions.Commands;

public record AddQuestionCommand(Guid QuizId, string Text, int TimeLimitSeconds, int Order) : IRequest<Guid>;
