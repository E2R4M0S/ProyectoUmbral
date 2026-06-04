using MediatR;

namespace Trivia.Application.Trivias.QuizBank;

public record ListQuizzesQuery : IRequest<List<QuizListItemDto>>;

public record QuizListItemDto(Guid Id, string Title, int QuestionCount);
