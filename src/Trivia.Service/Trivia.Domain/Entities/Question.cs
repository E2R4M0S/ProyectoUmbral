using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain;

public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public required string Text { get; set; }

    public int TimeLimitSeconds { get; set; } = 30;

    public int Order { get; set; }

    public List<Answer> Answers { get; set; } = new();
}
