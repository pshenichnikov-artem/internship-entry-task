using AutoMapper;
using Core.Models.DTOs.Game;
using Core.Models.DTOs.Move;
using Core.Models.DTOs.Player;
using Core.Models.Entities;

namespace Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Player, PlayerResponse>();
            CreateMap<PlayerAddRequest, Player>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<Game, GameResponse>()
                .ForMember(dest => dest.Moves, opt => opt.MapFrom(src => src.Moves));
            CreateMap<GameAddRequest, Game>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Size, opt => opt.Ignore())
                .ForMember(dest => dest.WinConditionLength, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentTurn, opt => opt.Ignore())
                .ForMember(dest => dest.Winner, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.EndedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Moves, opt => opt.Ignore());

            CreateMap<Move, MoveResponse>();
            CreateMap<Move, MoveInfo>();
            CreateMap<MoveAddRequest, Move>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Symbol, opt => opt.Ignore())
                .ForMember(dest => dest.MoveNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsReplacedByOpponentSymbol, opt => opt.Ignore());
        }
    }
}