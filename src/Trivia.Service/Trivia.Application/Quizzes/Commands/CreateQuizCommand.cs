using MediatR;

namespace Trivia.Application.Quizzes.Commands;

public record CreateQuizCommand(string Name, string? Description) : IRequest<Guid>;
