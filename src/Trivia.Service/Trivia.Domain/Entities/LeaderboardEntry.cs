using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain.Entities;

public class LeaderboardEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid QuizId { get; set; }
    public Guid TeamId { get; set; }
    public int Score { get; set; }
    public DateTime UpdatedAt { get; set; }
}
