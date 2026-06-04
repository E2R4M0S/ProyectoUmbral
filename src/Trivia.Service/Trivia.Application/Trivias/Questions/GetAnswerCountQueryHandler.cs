using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Questions;

public class GetAnswerCountQueryHandler : IRequestHandler<GetAnswerCountQuery, int>
{
    private readonly IParticipantAnswerRepository _repo;

    public GetAnswerCountQueryHandler(IParticipantAnswerRepository repo) => _repo = repo;

    public async Task<int> Handle(GetAnswerCountQuery request, CancellationToken ct)
    {
        return await _repo.GetCountByQuestionAsync(request.QuestionId, ct);
    }
}
