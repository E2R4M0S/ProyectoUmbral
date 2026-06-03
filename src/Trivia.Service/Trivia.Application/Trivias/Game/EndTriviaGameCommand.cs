using MediatR;

namespace Trivia.Application.Trivias.Game;

public record EndTriviaGameCommand(Guid SessionId) : IRequest;
