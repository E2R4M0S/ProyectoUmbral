using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Trivia.Domain.Entities;
using Trivia.Infrastructure.Persistence;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class QuestionRepositoryTests : DbTestBase
{
    [Fact]
    public async Task AddRangeAsync_PersistsQuestions()
    {
        var ctx = CreateContext();
        var repo = new QuestionRepository(ctx);
        var quizId = Guid.NewGuid();

        var questions = new List<Question>
        {
            new() { QuizId = quizId, Text = "Q1", TimeLimitSeconds = 30 },
            new() { QuizId = quizId, Text = "Q2", TimeLimitSeconds = 20 }
        };

        await repo.AddRangeAsync(questions);

        var ctx2 = CreateContext();
        var saved = ctx2.Set<Question>().Where(q => q.QuizId == quizId).ToList();
        saved.Should().HaveCount(2);
        saved.Should().Contain(q => q.Text == "Q1");
        saved.Should().Contain(q => q.Text == "Q2");
    }

    [Fact]
    public async Task AddRangeAsync_EmptyList_SavesNothing()
    {
        var ctx = CreateContext();
        var repo = new QuestionRepository(ctx);
        var quizId = Guid.NewGuid();

        await repo.AddRangeAsync(new List<Question>());

        var ctx2 = CreateContext();
        var saved = ctx2.Set<Question>().Where(q => q.QuizId == quizId).ToList();
        saved.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRangeAsync_AssignsIdsAutomatically()
    {
        var ctx = CreateContext();
        var repo = new QuestionRepository(ctx);
        var quizId = Guid.NewGuid();

        var questions = new List<Question>
        {
            new() { QuizId = quizId, Text = "Only Q", TimeLimitSeconds = 15 }
        };

        await repo.AddRangeAsync(questions);

        questions[0].Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task AddRangeAsync_MultipleQuizzes_IsolatedCorrectly()
    {
        var ctx = CreateContext();
        var repo = new QuestionRepository(ctx);
        var quiz1 = Guid.NewGuid();
        var quiz2 = Guid.NewGuid();

        await repo.AddRangeAsync(new List<Question>
        {
            new() { QuizId = quiz1, Text = "Quiz1 Q1", TimeLimitSeconds = 10 }
        });
        await repo.AddRangeAsync(new List<Question>
        {
            new() { QuizId = quiz2, Text = "Quiz2 Q1", TimeLimitSeconds = 10 },
            new() { QuizId = quiz2, Text = "Quiz2 Q2", TimeLimitSeconds = 10 }
        });

        var ctx2 = CreateContext();
        ctx2.Set<Question>().Where(q => q.QuizId == quiz1).Should().HaveCount(1);
        ctx2.Set<Question>().Where(q => q.QuizId == quiz2).Should().HaveCount(2);
    }
}
