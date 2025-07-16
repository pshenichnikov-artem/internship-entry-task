using Core.Models.DTOs.Common;
using Core.Models.Entities;
using Core.Models.Enums;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.Implementation
{
    public class GameRepository : IGameRepository
    {
        private readonly ApplicationDbContext _context;

        public GameRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Game> AddGameAsync(Game game)
        {
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return game;
        }

        public async Task<Game?> GetGameAsync(Guid id)
        {
            return await _context.Games
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<PagedResponse<Game>> SearchGamesAsync(int pageNumber, int pageSize, GameStatus? status = null, List<Guid>? playerIds = null, List<(string field, SortDirection direction)>? sorts = null)
        {
            IQueryable<Game> query = _context.Games.Include(g => g.Moves);

            if (status != null)
                query = query.Where(g => g.Status == status);

            if (playerIds != null && playerIds.Any())
                query = query.Where(g => playerIds.Contains(g.PlayerXId) || playerIds.Contains(g.PlayerOId));

            sorts = sorts?.Any() == true ? sorts : new List<(string field, SortDirection direction)> { (field: "CreatedAt", direction: SortDirection.Ascending) };
            query = SortingHelper.ApplySorting(query, sorts);

            int totalCount = await query.CountAsync();
            int skip = (pageNumber - 1) * pageSize;

            var games = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<Game>
            {
                Items = games,
                TotalCount = totalCount,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }

        public async Task UpdateGameAsync(Game game)
        {
            _context.Games.Update(game);
            await _context.SaveChangesAsync();
        }


    }
}