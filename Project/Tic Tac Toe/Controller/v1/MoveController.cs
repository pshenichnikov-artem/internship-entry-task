using Core.Interfaces.ServiceInterfaces;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Move;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tic_Tac_Toe.Controller.v1
{
    [ApiVersion("1.0")]
    public class MoveController : CustomBaseController
    {
        private readonly IMoveService _moveService;

        public MoveController(IMoveService moveService)
        {
            _moveService = moveService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddMove([FromBody] MoveAddRequest request)
        {
            request.PlayerId = UserId;

            var result = await _moveService.AddMoveAsync(request);
            if (result.Success &&
                result.Metadata != null &&
                result.Metadata.TryGetValue("ETag", out var etagObj) &&
                etagObj is string etag)
            {
                Response.Headers.ETag = $"\"{etag}\"";
            }
            return ApiResponse.FromServiceResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMove(Guid id)
        {
            var result = await _moveService.GetMoveAsync(id);
            return ApiResponse.FromServiceResult(result);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchMoves([FromBody] MoveSearchRequest request)
        {
            var result = await _moveService.SearchMovesAsync(request);
            return ApiResponse.FromServiceResult(result);
        }
    }
}