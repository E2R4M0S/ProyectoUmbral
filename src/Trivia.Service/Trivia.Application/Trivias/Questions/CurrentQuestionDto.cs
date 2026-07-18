namespace Trivia.Application.Trivias.Questions;

public record CurrentQuestionDto(
    Guid QuestionId,
    string QuestionText,
    string[] Options,
    int TimeLimitSeconds,
    DateTime AskedAt);
