using MediatR;

namespace Trivia.Application.Trivias.Clues;

public record ReleaseClueCommand(Guid QuizId, Guid? TeamId, object ClueData) : IRequest;
