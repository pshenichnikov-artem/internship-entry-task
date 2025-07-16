using Core.Interfaces.ServiceInterfaces;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tic_Tac_Toe.Controller.v1
{
    [ApiVersion("1.0")]
    public class GameController : CustomBaseController
    {
        private readonly IGameService _gameService;

        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddGame([FromBody] GameAddRequest request)
        {
            if(UserId != request.PlayerOId && UserId != request.PlayerXId)
            {
                return ApiResponse.BadRequest("Нельзя создать игру не учавствя в ней");
            }

            var result = await _gameService.AddGameAsync(request);
            return ApiResponse.FromServiceResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGame(Guid id)
        {
            var result = await _gameService.GetGameAsync(id);
            return ApiResponse.FromServiceResult(result);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchGames([FromBody] GameSearchRequest request)
        {
            var result = await _gameService.SearchGamesAsync(request);
            return ApiResponse.FromServiceResult(result);
        }
    }
}