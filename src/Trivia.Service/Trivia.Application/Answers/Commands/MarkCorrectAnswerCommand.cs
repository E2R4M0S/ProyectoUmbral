using MediatR;

namespace Trivia.Application.Answers.Commands;

public record MarkCorrectAnswerCommand(Guid QuizId, Guid QuestionId, Guid AnswerId) : IRequest<MediatR.Unit>;
