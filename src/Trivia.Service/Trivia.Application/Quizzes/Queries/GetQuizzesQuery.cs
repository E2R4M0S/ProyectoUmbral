using MediatR;

namespace Trivia.Application.Quizzes.Queries;

public record GetQuizzesQuery(string? Q = null, int Page = 1, int Size = 20) : IRequest<IEnumerable<object>>;
