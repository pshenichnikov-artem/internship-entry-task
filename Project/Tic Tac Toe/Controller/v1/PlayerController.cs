using Core.Interfaces.ServiceInterfaces;
using Core.Models.DTOs.Auth;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Player;
using Microsoft.AspNetCore.Mvc;

namespace Tic_Tac_Toe.Controller.v1
{
    [ApiVersion("1.0")]
    public class PlayerController : CustomBaseController
    {
        private readonly IPlayerService _playerService;

        public PlayerController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _playerService.RegisterAsync(request);
            return ApiResponse.FromServiceResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _playerService.AuthenticateAsync(request);
            return ApiResponse.FromServiceResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(Guid id)
        {
            var result = await _playerService.GetPlayerAsync(id);
            return ApiResponse.FromServiceResult(result);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchPlayers([FromBody] PlayerSearchRequest request)
        {
            var result = await _playerService.SearchPlayersAsync(request);
            return ApiResponse.FromServiceResult(result);
        }
    }
}