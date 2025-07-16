using Core.Models;
using Core.Models.DTOs.Auth;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Player;

namespace Core.Interfaces.ServiceInterfaces
{
    public interface IPlayerService
    {
        Task<ServiceResult<string>> AuthenticateAsync(LoginRequest request);
        Task<ServiceResult<string>> RegisterAsync(RegisterRequest request);
        Task<ServiceResult<PlayerResponse>> GetPlayerAsync(Guid id);
        Task<ServiceResult<PagedResponse<PlayerResponse>>> SearchPlayersAsync(PlayerSearchRequest request);
    }
}