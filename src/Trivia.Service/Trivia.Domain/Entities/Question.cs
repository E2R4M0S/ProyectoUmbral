using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain.Entities;

public class Question
{
    [Key]
    public Guid Id { get; set; }

    public Guid QuizId { get; set; }

    public int TimeLimitSeconds { get; set; }

    public DateTime? ReleasedAt { get; set; }
}
