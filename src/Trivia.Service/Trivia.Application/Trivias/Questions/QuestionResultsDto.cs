namespace Trivia.Application.Trivias.Questions;

public record AnswerResultDto(Guid AnswerId, string Text, int Count, double Percentage);

public record QuestionResultsDto(Guid QuizId, Guid QuestionId, List<AnswerResultDto> Results);
