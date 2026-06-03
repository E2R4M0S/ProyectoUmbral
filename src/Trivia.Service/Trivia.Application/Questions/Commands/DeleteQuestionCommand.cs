using MediatR;

namespace Trivia.Application.Questions.Commands;

public record DeleteQuestionCommand(Guid QuizId, Guid QuestionId) : IRequest<MediatR.Unit>;
