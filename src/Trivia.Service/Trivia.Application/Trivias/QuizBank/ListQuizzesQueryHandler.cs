using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.QuizBank;

public class ListQuizzesQueryHandler : IRequestHandler<ListQuizzesQuery, List<QuizListItemDto>>
{
    private readonly IQuizRepository _quizRepo;

    public ListQuizzesQueryHandler(IQuizRepository quizRepo) => _quizRepo = quizRepo;

    public async Task<List<QuizListItemDto>> Handle(ListQuizzesQuery request, CancellationToken ct)
    {
        var quizzes = await _quizRepo.GetAllAsync(ct);
        var result = new List<QuizListItemDto>();

        foreach (var quiz in quizzes)
        {
            var questions = await _quizRepo.GetQuestionsAsync(quiz.Id, ct);
            result.Add(new QuizListItemDto(quiz.Id, quiz.Title ?? "", questions.Count));
        }

        return result;
    }
}
