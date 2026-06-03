using MediatR;

namespace Trivia.Application.Answers.Commands;

public record AddAnswerCommand(Guid QuizId, Guid QuestionId, string Text) : IRequest<Guid>;
