using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain.Entities;

public class Quiz
{
    [Key]
    public Guid Id { get; set; }

    public string? Title { get; set; }

    // Additional properties are intentionally minimal for now.
}
