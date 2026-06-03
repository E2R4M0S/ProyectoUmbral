using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Questions;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Trivias.Questions;

public class QuestionResultsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCalculateResultsAndPublish()
    {
        var answerRepo = Substitute.For<IAnswerRepository>();
        var participantRepo = Substitute.For<IParticipantAnswerRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<QuestionResultsCommandHandler>>();
        var handler = new QuestionResultsCommandHandler(answerRepo, participantRepo, publisher, logger);

        var quizId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerA = Guid.NewGuid();
        var answerB = Guid.NewGuid();

        answerRepo.GetByQuestionIdAsync(questionId, Arg.Any<CancellationToken>())
            .Returns(new List<Answer>
            {
                new() { Id = answerA, Text = "A", IsCorrect = true },
                new() { Id = answerB, Text = "B", IsCorrect = false }
            });

        participantRepo.GetByQuizAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new List<ParticipantAnswer>
            {
                new() { QuizId = quizId, QuestionId = questionId, AnswerId = answerA, IsCorrect = true },
                new() { QuizId = quizId, QuestionId = questionId, AnswerId = answerB, IsCorrect = false },
                new() { QuizId = quizId, QuestionId = questionId, AnswerId = answerA, IsCorrect = true }
            });

        var cmd = new QuestionResultsCommand(quizId, questionId);
        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<string>(s => s == "QuestionResultsUpdated"),
            Arg.Is<QuestionResultsDto>(dto =>
                dto.QuizId == quizId &&
                dto.QuestionId == questionId &&
                dto.Results.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoParticipants_ShouldReturnZeroPercentages()
    {
        var answerRepo = Substitute.For<IAnswerRepository>();
        var participantRepo = Substitute.For<IParticipantAnswerRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<QuestionResultsCommandHandler>>();
        var handler = new QuestionResultsCommandHandler(answerRepo, participantRepo, publisher, logger);

        var quizId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        answerRepo.GetByQuestionIdAsync(questionId, Arg.Any<CancellationToken>())
            .Returns(new List<Answer> { new() { Id = Guid.NewGuid(), Text = "A", IsCorrect = true } });

        participantRepo.GetByQuizAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new List<ParticipantAnswer>());

        var cmd = new QuestionResultsCommand(quizId, questionId);
        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Is<QuestionResultsDto>(dto => dto.Results[0].Percentage == 0.0),
            Arg.Any<CancellationToken>());
    }
}
