using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain;

public class Answer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }

    public required string Text { get; set; }

    public bool IsCorrect { get; set; }
}
