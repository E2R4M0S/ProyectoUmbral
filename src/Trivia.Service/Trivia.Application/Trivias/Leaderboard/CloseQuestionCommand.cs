using MediatR;

namespace Trivia.Application.Trivias.Leaderboard;

public record CloseQuestionCommand(Guid QuizId) : IRequest;
