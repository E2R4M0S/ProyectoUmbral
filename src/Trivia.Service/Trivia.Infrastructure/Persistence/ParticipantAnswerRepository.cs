using Microsoft.EntityFrameworkCore;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Infrastructure.Persistence;

    public class ParticipantAnswerRepository : IParticipantAnswerRepository
    {
        private readonly TriviaDbContext _db;

        public ParticipantAnswerRepository(TriviaDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ParticipantAnswer answer, CancellationToken ct = default)
        {
            await _db.Set<ParticipantAnswer>().AddAsync(answer, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<ParticipantAnswer>> GetByQuizAsync(Guid quizId, CancellationToken ct = default)
        {
            return await _db.Set<ParticipantAnswer>().Where(a => a.QuizId == quizId).ToListAsync(ct);
        }
    }
