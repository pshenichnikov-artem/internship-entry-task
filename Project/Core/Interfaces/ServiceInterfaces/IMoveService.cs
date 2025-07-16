using Core.Models;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Move;

namespace Core.Interfaces.ServiceInterfaces
{
    public interface IMoveService
    {
        Task<ServiceResult<MoveResponse>> AddMoveAsync(MoveAddRequest request);
        Task<ServiceResult<MoveResponse>> GetMoveAsync(Guid id);
        Task<ServiceResult<PagedResponse<MoveResponse>>> SearchMovesAsync(MoveSearchRequest request);
    }
}