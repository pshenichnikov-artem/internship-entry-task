using Core.Models;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;

namespace Core.Interfaces.ServiceInterfaces
{
    public interface IGameService
    {
        Task<ServiceResult<GameResponse>> AddGameAsync(GameAddRequest request);
        Task<ServiceResult<GameResponse>> GetGameAsync(Guid id);
        Task<ServiceResult<PagedResponse<GameResponse>>> SearchGamesAsync(GameSearchRequest request);
    }
}