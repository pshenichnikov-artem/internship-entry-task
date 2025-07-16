using Core.Models.DTOs.Common;
using Core.Models.Entities;
using Core.Models.Enums;

namespace Infrastructure.Repositories.Interfaces
{
    public interface IMoveRepository
    {
        Task<Move> AddMoveAsync(Move move);
        Task<Move?> GetMoveByClientMoveIdAsync(Guid gameId, string clientMoveId);
        Task<Move?> GetMoveAsync(Guid id);
        Task<PagedResponse<Move>> SearchMovesAsync(int pageNumber, int pageSize, Guid? gameId = null, Guid? playerId = null, List<(string field, SortDirection direction)>? sorts = null);
    }
}