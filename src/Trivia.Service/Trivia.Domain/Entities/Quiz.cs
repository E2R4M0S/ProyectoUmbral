using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain;

public class Quiz
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    public List<Question> Questions { get; set; } = new();
}
