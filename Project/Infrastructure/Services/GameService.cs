using AutoMapper;
using Core.Interfaces.ServiceInterfaces;
using Core.Models;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;
using Core.Models.Entities;
using Core.Models.Enums;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Core.Services
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public GameService(IGameRepository gameRepository, IPlayerRepository playerRepository, IConfiguration configuration, IMapper mapper)
        {
            _gameRepository = gameRepository;
            _playerRepository = playerRepository;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<ServiceResult<GameResponse>> AddGameAsync(GameAddRequest request)
        {
            try
            {
                var playerX = await _playerRepository.GetPlayerAsync(request.PlayerXId);
                if (playerX == null)
                {
                    return ServiceResult<GameResponse>.ErrorResult(404, "Игрок X не найден");
                }

                var playerO = await _playerRepository.GetPlayerAsync(request.PlayerOId);
                if (playerO == null)
                {
                    return ServiceResult<GameResponse>.ErrorResult(404, "Игрок O не найден");
                }

                if (request.PlayerXId == request.PlayerOId)
                {
                    return ServiceResult<GameResponse>.ErrorResult(400, "Игрок не может играть сам с собой");
                }

                var boardSize = int.Parse(_configuration["Game:BoardSize"] ?? "3");
                var winConditionLength = int.Parse(_configuration["Game:WinConditionLength"] ?? "3");
                
                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    PlayerXId = request.PlayerXId,
                    PlayerOId = request.PlayerOId,
                    Size = boardSize,
                    WinConditionLength = winConditionLength,
                    Status = GameStatus.InProgress,
                    CurrentTurn = PlayerSymbol.X,
                    CreatedAt = DateTime.UtcNow
                };
                
                var createdGame = await _gameRepository.AddGameAsync(game);
                return ServiceResult<GameResponse>.SuccessResult(_mapper.Map<GameResponse>(createdGame));
            }
            catch (Exception ex)
            {
                return ServiceResult<GameResponse>.ErrorResult(500, ex.Message);
            }
        }

        public async Task<ServiceResult<GameResponse>> GetGameAsync(Guid id)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(id);
                
                if (game == null)
                {
                    return ServiceResult<GameResponse>.ErrorResult(404, "Игра не найдена");
                }
                
                return ServiceResult<GameResponse>.SuccessResult(_mapper.Map<GameResponse>(game));
            }
            catch (Exception ex)
            {
                return ServiceResult<GameResponse>.ErrorResult(500, ex.Message);
            }
        }

        public async Task<ServiceResult<PagedResponse<GameResponse>>> SearchGamesAsync(GameSearchRequest request)
        {
            try
            {
                var sorts = request.Sort?.Select(s => (s.Field, s.Direction)).ToList();
                var games = await _gameRepository.SearchGamesAsync(
                    request.Pagination.PageNumber,
                    request.Pagination.PageSize,
                    request.Filter?.Status,
                    request.Filter?.PlayerIds,
                    sorts);
                var mappedGames = new PagedResponse<GameResponse>
                {
                    Items = _mapper.Map<List<GameResponse>>(games.Items),
                    TotalCount = games.TotalCount,
                    PageSize = games.PageSize,
                    PageNumber = games.PageNumber
                };
                return ServiceResult<PagedResponse<GameResponse>>.SuccessResult(mappedGames);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResponse<GameResponse>>.ErrorResult(500, ex.Message);
            }
        }
    }
}