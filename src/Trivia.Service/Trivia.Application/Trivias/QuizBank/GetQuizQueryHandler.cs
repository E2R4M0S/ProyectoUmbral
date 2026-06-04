using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.QuizBank;

public class GetQuizQueryHandler : IRequestHandler<GetQuizQuery, QuizDetailDto?>
{
    private readonly IQuizRepository _quizRepo;

    public GetQuizQueryHandler(IQuizRepository quizRepo) => _quizRepo = quizRepo;

    public async Task<QuizDetailDto?> Handle(GetQuizQuery request, CancellationToken ct)
    {
        var quiz = await _quizRepo.GetByIdAsync(request.Id, ct);
        if (quiz == null) return null;

        var questions = await _quizRepo.GetQuestionsAsync(quiz.Id, ct);
        var questionDtos = new List<QuestionDetailDto>();

        foreach (var q in questions)
        {
            var answers = await _quizRepo.GetAnswersAsync(q.Id, ct);
            questionDtos.Add(new QuestionDetailDto(
                q.Id,
                q.Text ?? "",
                q.TimeLimitSeconds,
                answers.Select(a => new AnswerDetailDto(a.Id, a.Text ?? "", a.IsCorrect)).ToList()
            ));
        }

        return new QuizDetailDto(quiz.Id, quiz.Title ?? "", questionDtos);
    }
}
