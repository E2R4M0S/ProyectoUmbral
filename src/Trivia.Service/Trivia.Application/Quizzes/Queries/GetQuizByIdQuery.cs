using MediatR;

namespace Trivia.Application.Quizzes.Queries;

public record GetQuizByIdQuery(Guid QuizId) : IRequest<object?>;
