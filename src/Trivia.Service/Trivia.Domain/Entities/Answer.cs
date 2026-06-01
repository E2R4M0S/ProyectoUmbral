using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain.Entities;

public class Answer
{
    [Key]
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public string? Text { get; set; }

    public bool IsCorrect { get; set; }
}
