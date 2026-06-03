using MediatR;

namespace Trivia.Application.Trivias.Leaderboard;

public record CloseQuestionCommand(Guid QuizId, Guid QuestionId) : IRequest;
