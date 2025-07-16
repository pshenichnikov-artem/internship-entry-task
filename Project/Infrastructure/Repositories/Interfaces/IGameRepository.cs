using Core.Models.DTOs.Common;
using Core.Models.Entities;
using Core.Models.Enums;

namespace Infrastructure.Repositories.Interfaces
{
    public interface IGameRepository
    {
        Task<Game> AddGameAsync(Game game);
        Task<Game?> GetGameAsync(Guid id);
        Task<PagedResponse<Game>> SearchGamesAsync(int pageNumber, int pageSize, GameStatus? status = null, List<Guid>? playerIds = null, List<(string field, SortDirection direction)>? sorts = null);
        Task UpdateGameAsync(Game game);
    }
}