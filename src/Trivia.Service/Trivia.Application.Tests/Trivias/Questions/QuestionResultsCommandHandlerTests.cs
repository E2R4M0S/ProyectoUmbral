using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Trivia.Application.Trivias.Questions;
using Trivia.Application.Common.Interfaces;
using Xunit;

namespace Trivia.Application.Tests.Trivias.Questions;

public class QuestionResultsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ComputesCountsAndPercentages_AndPublishes()
    {
        var answerRepo = Substitute.For<IAnswerRepository>();
        var participantRepo = Substitute.For<IParticipantAnswerRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<QuestionResultsCommandHandler>>();

        var qid = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var a1 = new Domain.Entities.Answer { Id = Guid.NewGuid(), QuestionId = questionId, Text = "A", IsCorrect = true };
        var a2 = new Domain.Entities.Answer { Id = Guid.NewGuid(), QuestionId = questionId, Text = "B", IsCorrect = false };

        answerRepo.GetByQuestionIdAsync(questionId, Arg.Any<CancellationToken>()).Returns(new List<Domain.Entities.Answer> { a1, a2 });

        var pa = new List<Domain.Entities.ParticipantAnswer>
        {
            new Domain.Entities.ParticipantAnswer { Id = Guid.NewGuid(), QuizId = qid, QuestionId = questionId, AnswerId = a1.Id, TeamId = Guid.NewGuid(), IsCorrect = true, Timestamp = DateTime.UtcNow },
            new Domain.Entities.ParticipantAnswer { Id = Guid.NewGuid(), QuizId = qid, QuestionId = questionId, AnswerId = a2.Id, TeamId = Guid.NewGuid(), IsCorrect = false, Timestamp = DateTime.UtcNow },
            new Domain.Entities.ParticipantAnswer { Id = Guid.NewGuid(), QuizId = qid, QuestionId = questionId, AnswerId = a1.Id, TeamId = Guid.NewGuid(), IsCorrect = true, Timestamp = DateTime.UtcNow }
        };

        participantRepo.GetByQuizAsync(qid, Arg.Any<CancellationToken>()).Returns(pa);

        var handler = new QuestionResultsCommandHandler(answerRepo, participantRepo, publisher, logger);

        await handler.Handle(new QuestionResultsCommand(qid, questionId), CancellationToken.None);

        await publisher.Received(1).PublishAsync("QuestionResultsUpdated", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
