using MediatR;

namespace Trivia.Application.Trivias.Questions;

public class GetAnswerCountQueryHandler : IRequestHandler<GetAnswerCountQuery, int>
{
    public Task<int> Handle(GetAnswerCountQuery request, CancellationToken ct)
    {
        // Count unique teams that answered this question (team-based, not per-participant)
        var count = AskQuestionCommandHandler.TeamAnswers.Keys
            .Count(k => k.Item1 == request.QuestionId);
        return Task.FromResult(count);
    }
}
