using MediatR;

namespace Trivia.Application.Trivias.QuizBank;

public record CreateQuizCommand(string Title, List<QuestionInput> Questions) : IRequest<Guid>;

public record QuestionInput(string Text, List<AnswerInput> Answers);

public record AnswerInput(string Text, bool IsCorrect);
