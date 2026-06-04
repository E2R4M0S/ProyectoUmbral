using MediatR;

namespace Trivia.Application.Trivias.Questions;

public record GetAnswerCountQuery(Guid QuestionId) : IRequest<int>;
