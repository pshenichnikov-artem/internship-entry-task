using Core.Models.DTOs.Common;
using Core.Models.Entities;
using Core.Models.Enums;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.Implementation
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly ApplicationDbContext _context;

        public PlayerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Player> AddPlayerAsync(Player player)
        {
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        public async Task<Player?> GetPlayerAsync(Guid id)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            return player;
        }

        public async Task<Player?> GetPlayerByUsernameAsync(string username)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Username.ToLower() == username.ToLower());
            return player;
        }

        public async Task<PagedResponse<Player>> SearchPlayersAsync(int pageNumber, int pageSize, List<string>? usernames = null, List<(string field, SortDirection direction)>? sorts = null)
        {
            IQueryable<Player> query = _context.Players;

            if (usernames != null && usernames.Any())
            {
                var lowercaseUsernames = usernames.Select(u => u.ToLower()).ToList();
                query = query.Where(p => lowercaseUsernames.Contains(p.Username.ToLower()));
            }

            sorts = sorts?.Any() == true ? sorts : new List<(string field, SortDirection direction)> { ("username", SortDirection.Ascending) };
            query = SortingHelper.ApplySorting( query, sorts);

            int totalCount = await query.CountAsync();
            int skip = (pageNumber - 1) * pageSize;

            var players = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<Player>
            {
                Items = players,
                TotalCount = totalCount,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }




    }
}