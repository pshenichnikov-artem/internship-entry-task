using Core.Models.DTOs.Common;
using Core.Models.Entities;
using Core.Models.Enums;

namespace Infrastructure.Repositories.Interfaces
{
    public interface IPlayerRepository
    {
        Task<Player> AddPlayerAsync(Player player);
        Task<Player?> GetPlayerAsync(Guid id);
        Task<Player?> GetPlayerByUsernameAsync(string username);
        Task<PagedResponse<Player>> SearchPlayersAsync(int pageNumber, int pageSize, List<string>? usernames = null, List<(string field, SortDirection direction)>? sorts = null);
    }
}