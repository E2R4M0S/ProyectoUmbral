using MediatR;

namespace Trivia.Application.Trivias.Leaderboard;

public record CloseQuestionCommand(Guid SessionId, Guid QuestionId) : IRequest;
