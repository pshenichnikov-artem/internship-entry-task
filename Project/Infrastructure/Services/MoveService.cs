using AutoMapper;
using Core.Interfaces.ServiceInterfaces;
using Core.Models;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Move;
using Core.Models.Entities;
using Core.Models.Enums;
using Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Core.Services
{
    public class MoveService : IMoveService
    {
        private readonly IMoveRepository _moveRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IMapper _mapper;

        public MoveService(IMoveRepository moveRepository, IGameRepository gameRepository, IPlayerRepository playerRepository, IMapper mapper)
        {
            _moveRepository = moveRepository;
            _gameRepository = gameRepository;
            _playerRepository = playerRepository;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MoveResponse>> AddMoveAsync(MoveAddRequest request)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(request.GameId);
                if (game == null)
                    return ServiceResult<MoveResponse>.ErrorResult(404, "Игра не найдена");

                var existingMove = await _moveRepository.GetMoveByClientMoveIdAsync(request.GameId, request.ClientMoveId);
                if (existingMove != null)
                {
                    var generatedEtag = GenerateETag(existingMove);
                    return ServiceResult<MoveResponse>.SuccessResult(
                        _mapper.Map<MoveResponse>(existingMove),
                        new Dictionary<string, object> { ["ETag"] = generatedEtag });
                }

                if (game.Status == Core.Models.Enums.GameStatus.Finished)
                    return ServiceResult<MoveResponse>.ErrorResult(409, "Игра уже завершена");

                var player = await _playerRepository.GetPlayerAsync(request.PlayerId.Value);
                if (player == null)
                    return ServiceResult<MoveResponse>.ErrorResult(404, "Игрок не найден");

                if (game.PlayerXId != request.PlayerId && game.PlayerOId != request.PlayerId)
                    return ServiceResult<MoveResponse>.ErrorResult(403, "Игрок не участвует в этой игре");

                var playerSymbol = game.PlayerXId == request.PlayerId ? Core.Models.Enums.PlayerSymbol.X : Core.Models.Enums.PlayerSymbol.O;
                if (game.CurrentTurn != playerSymbol)
                    return ServiceResult<MoveResponse>.ErrorResult(409, "Сейчас не ваш ход");

                if (request.X < 0 || request.X >= game.Size || request.Y < 0 || request.Y >= game.Size)
                    return ServiceResult<MoveResponse>.ErrorResult(400, "Координаты хода выходят за пределы игрового поля");

                if (game.Moves.Any(m => m.X == request.X && m.Y == request.Y))
                    return ServiceResult<MoveResponse>.ErrorResult(409, "Эта клетка уже занята");

                var move = new Move
                {
                    Id = Guid.NewGuid(),
                    GameId = request.GameId,
                    PlayerId = request.PlayerId.Value,
                    Symbol = playerSymbol,
                    X = request.X,
                    Y = request.Y,
                    MoveNumber = game.Moves.Count + 1,
                    CreatedAt = DateTime.UtcNow,
                    ClientMoveId = request.ClientMoveId,
                    IsReplacedByOpponentSymbol = false
                };

                if (move.MoveNumber % 3 == 0 && Random.Shared.Next(100) < 10)
                {
                    move.Symbol = move.Symbol == PlayerSymbol.X ? PlayerSymbol.O : PlayerSymbol.X;
                    move.IsReplacedByOpponentSymbol = true;
                }

                var createdMove = await _moveRepository.AddMoveAsync(move);

                game.Moves.Add(move);
                game.CurrentTurn = game.CurrentTurn == PlayerSymbol.X ? PlayerSymbol.O : PlayerSymbol.X;

                var winner = CheckWinner(game, move);
                if (winner != null)
                {
                    game.Status = GameStatus.Finished;
                    game.Winner = winner;
                    game.EndedAt = DateTime.UtcNow;
                }
                else if (game.Moves.Count == game.Size * game.Size)
                {
                    game.Status = GameStatus.Finished;
                    game.EndedAt = DateTime.UtcNow;
                }

                await _gameRepository.UpdateGameAsync(game);

                var etag = GenerateETag(createdMove);
                return ServiceResult<MoveResponse>.SuccessResult(
                    _mapper.Map<MoveResponse>(createdMove),
                    new Dictionary<string, object> { ["ETag"] = etag });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Moves_GameId_ClientMoveId") == true)
            {
                var move = await _moveRepository.GetMoveByClientMoveIdAsync(request.GameId, request.ClientMoveId);
                var generatedEtag = GenerateETag(move);
                return ServiceResult<MoveResponse>.SuccessResult(
                    _mapper.Map<MoveResponse>(move),
                    new Dictionary<string, object> { ["ETag"] = generatedEtag });
            }
            catch (Exception ex)
            {
                return ServiceResult<MoveResponse>.ErrorResult(500, ex.Message);
            }
        }

        private string GenerateETag(Move move)
        {
            var payload = $"{move.Id}:{move.GameId}:{move.PlayerId}:{move.Symbol}:{move.X}:{move.Y}:{move.MoveNumber}:{move.CreatedAt.Ticks}:{move.IsReplacedByOpponentSymbol}";

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return Convert.ToHexString(hashBytes);
        }

        public async Task<ServiceResult<MoveResponse>> GetMoveAsync(Guid id)
        {
            try
            {
                var move = await _moveRepository.GetMoveAsync(id);
                
                if (move == null)
                {
                    return ServiceResult<MoveResponse>.ErrorResult(404, "Ход не найден");
                }
                
                return ServiceResult<MoveResponse>.SuccessResult(_mapper.Map<MoveResponse>(move));
            }
            catch (Exception ex)
            {
                return ServiceResult<MoveResponse>.ErrorResult(500, ex.Message);
            }
        }

        public async Task<ServiceResult<PagedResponse<MoveResponse>>> SearchMovesAsync(MoveSearchRequest request)
        {
            try
            {
                var sorts = request.Sort?.Select(s => (s.Field, s.Direction)).ToList();
                var moves = await _moveRepository.SearchMovesAsync(
                    request.Pagination.PageNumber,
                    request.Pagination.PageSize,
                    request.Filter?.GameId,
                    request.Filter?.PlayerId,
                    sorts);
                var mappedMoves = new PagedResponse<MoveResponse>
                {
                    Items = _mapper.Map<List<MoveResponse>>(moves.Items),
                    TotalCount = moves.TotalCount,
                    PageSize = moves.PageSize,
                    PageNumber = moves.PageNumber
                };
                return ServiceResult<PagedResponse<MoveResponse>>.SuccessResult(mappedMoves);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResponse<MoveResponse>>.ErrorResult(500, ex.Message);
            }
        }

        private PlayerSymbol? CheckWinner(Game game, Move lastMove)
        {
            var playerMoves = game.Moves.Where(m => m.Symbol == lastMove.Symbol).ToList();

            if (CheckLine(playerMoves, lastMove.X, lastMove.Y, 1, 0, game.WinConditionLength))
                return lastMove.Symbol;

            if (CheckLine(playerMoves, lastMove.X, lastMove.Y, 0, 1, game.WinConditionLength))
                return lastMove.Symbol;

            if (CheckLine(playerMoves, lastMove.X, lastMove.Y, 1, 1, game.WinConditionLength))
                return lastMove.Symbol;

            if (CheckLine(playerMoves, lastMove.X, lastMove.Y, 1, -1, game.WinConditionLength))
                return lastMove.Symbol;

            return null;
        }

        private bool CheckLine(List<Move> playerMoves, int x, int y, int dx, int dy, int length)
        {
            int count = 1;

            for (int i = 1; i < length; i++)
            {
                if (playerMoves.Any(m => m.X == x + dx * i && m.Y == y + dy * i))
                    count++;
                else
                    break;
            }

            for (int i = 1; i < length; i++)
            {
                if (playerMoves.Any(m => m.X == x - dx * i && m.Y == y - dy * i))
                    count++;
                else
                    break;
            }

            return count >= length;
        }
    }
}