using MediatR;

namespace Trivia.Application.Trivias.QuizBank;

public record GetQuizQuery(Guid Id) : IRequest<QuizDetailDto?>;

public record QuizDetailDto(Guid Id, string Title, List<QuestionDetailDto> Questions);

public record QuestionDetailDto(Guid Id, string Text, int TimeLimitSeconds, List<AnswerDetailDto> Answers);

public record AnswerDetailDto(Guid Id, string Text, bool IsCorrect);
