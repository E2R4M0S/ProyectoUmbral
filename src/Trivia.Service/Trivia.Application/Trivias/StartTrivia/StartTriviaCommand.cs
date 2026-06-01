using MediatR;

namespace Trivia.Application.Trivias.StartTrivia;

public record StartTriviaCommand(Guid QuizId) : IRequest;
