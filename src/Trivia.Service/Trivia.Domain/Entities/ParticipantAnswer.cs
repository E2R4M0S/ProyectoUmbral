using System.ComponentModel.DataAnnotations;

namespace Trivia.Domain.Entities;

public class ParticipantAnswer
{
    [Key]
    public Guid Id { get; set; }

    public Guid QuizId { get; set; }
    public Guid TeamId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid AnswerId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsCorrect { get; set; }
}
