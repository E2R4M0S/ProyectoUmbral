using MediatR;

namespace Trivia.Application.Trivias.Questions;

public record QuestionResultsCommand(Guid QuizId, Guid QuestionId) : IRequest;
