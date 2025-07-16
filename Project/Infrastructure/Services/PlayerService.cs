using AutoMapper;
using Core.Interfaces.ServiceInterfaces;
using Core.Models;
using Core.Models.DTOs.Auth;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Player;
using Core.Models.Entities;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Utils;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Core.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IMapper _mapper;
        private readonly JwtTokenGenerator _jwtGenerator;

        public PlayerService(IPlayerRepository playerRepository, IMapper mapper, IConfiguration configuration)
        {
            _playerRepository = playerRepository;
            _mapper = mapper;
            _jwtGenerator = new JwtTokenGenerator(configuration);
        }

        public async Task<ServiceResult<string>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var existingPlayer = await _playerRepository.GetPlayerByUsernameAsync(request.Username);
                if (existingPlayer != null)
                    return ServiceResult<string>.ErrorResult(409, "Игрок уже существует");

                var player = new Player
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };
                
                var createdPlayer = await _playerRepository.AddPlayerAsync(player);
                var token = _jwtGenerator.GenerateToken(createdPlayer.Id.ToString(), createdPlayer.Username);
                return ServiceResult<string>.SuccessResult(token);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.ErrorResult(500, ex.Message);
            }
        }

        public async Task<ServiceResult<string>> AuthenticateAsync(LoginRequest request)
        {
            try
            {
                var player = await _playerRepository.GetPlayerByUsernameAsync(request.Username);
                if (player == null)
                    return ServiceResult<string>.ErrorResult(404, "Игрок не найден");

                if (!BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash))
                    return ServiceResult<string>.ErrorResult(401, "Неверный пароль");

                var token = _jwtGenerator.GenerateToken(player.Id.ToString(), player.Username);
                return ServiceResult<string>.SuccessResult(token);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.ErrorResult(500, ex.Message);
            }
        }

        public async Task<ServiceResult<PlayerResponse>> GetPlayerAsync(Guid id)
        {
            try
            {
                var player = await _playerRepository.GetPlayerAsync(id);
                
                if (player == null)
                {
                    return ServiceResult<PlayerResponse>.ErrorResult(404, "Игрок не найден");
                }
                
                return ServiceResult<PlayerResponse>.SuccessResult(_mapper.Map<PlayerResponse>(player));
            }
            catch (Exception ex)
            {
                return ServiceResult<PlayerResponse>.ErrorResult(500, ex.Message);
            }
        }

        public async Task<ServiceResult<PagedResponse<PlayerResponse>>> SearchPlayersAsync(PlayerSearchRequest request)
        {
            try
            {
                var players = await _playerRepository.SearchPlayersAsync(
                    request.Pagination.PageNumber,
                    request.Pagination.PageSize,
                    request.Filter?.Usernames,
                    request.Sort?.Select(s => (s.Field, s.Direction)).ToList());

                var mappedPlayers = new PagedResponse<PlayerResponse>
                {
                    Items = _mapper.Map<List<PlayerResponse>>(players.Items),
                    TotalCount = players.TotalCount,
                    PageSize = players.PageSize,
                    PageNumber = players.PageNumber
                };
                return ServiceResult<PagedResponse<PlayerResponse>>.SuccessResult(mappedPlayers);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResponse<PlayerResponse>>.ErrorResult(500, ex.Message);
            }
        }
    }
}