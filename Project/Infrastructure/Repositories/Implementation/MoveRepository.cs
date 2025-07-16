using Core.Models.DTOs.Common;
using Core.Models.Entities;
using Core.Models.Enums;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.Implementation
{
    public class MoveRepository : IMoveRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IGameRepository _gameRepository;

        public MoveRepository(ApplicationDbContext context, IGameRepository gameRepository)
        {
            _context = context;
            _gameRepository = gameRepository;
        }

        public async Task<Move> AddMoveAsync(Move move)
        {
            _context.Moves.Add(move);
            await _context.SaveChangesAsync();
            return move;
        }

        public async Task<Move?> GetMoveAsync(Guid id)
        {
            var move = await _context.Moves.FirstOrDefaultAsync(m => m.Id == id);
            return move;
        }

        public async Task<Move?> GetMoveByClientMoveIdAsync(Guid gameId, string clientMoveId)
        {
            return await _context.Moves
                .FirstOrDefaultAsync(m => m.GameId == gameId && m.ClientMoveId == clientMoveId);
        }

        public async Task<PagedResponse<Move>> SearchMovesAsync(int pageNumber, int pageSize, Guid? gameId = null, Guid? playerId = null, List<(string field, SortDirection direction)>? sorts = null)
        {
            IQueryable<Move> query = _context.Moves;

            if (gameId != null)
                query = query.Where(m => m.GameId == gameId);

            if (playerId != null)
                query = query.Where(m => m.PlayerId == playerId);

            sorts = sorts?.Any() == true ? sorts : new List<(string field, SortDirection direction)> { ("GameId", SortDirection.Ascending) };
            query = SortingHelper.ApplySorting(query, sorts);

            int totalCount = await query.CountAsync();

            int skip = (pageNumber - 1) * pageSize;

            var moves = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<Move>
            {
                Items = moves,
                TotalCount = totalCount,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }




    }
}