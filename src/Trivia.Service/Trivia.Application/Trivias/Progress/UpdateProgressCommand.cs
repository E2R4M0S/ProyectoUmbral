using MediatR;

namespace Trivia.Application.Trivias.Progress;

public record UpdateProgressCommand(Guid QuizId, object ProgressData) : IRequest;
