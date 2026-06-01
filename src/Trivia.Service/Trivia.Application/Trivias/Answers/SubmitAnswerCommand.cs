using MediatR;

namespace Trivia.Application.Trivias.Answers;

public record SubmitAnswerCommand(Guid QuizId, Guid TeamId, Guid QuestionId, Guid AnswerId, DateTime Timestamp) : IRequest;
