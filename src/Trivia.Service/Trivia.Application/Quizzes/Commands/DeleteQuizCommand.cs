using MediatR;

namespace Trivia.Application.Quizzes.Commands;

public record DeleteQuizCommand(Guid QuizId) : IRequest<MediatR.Unit>;
