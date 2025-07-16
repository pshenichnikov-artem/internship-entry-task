using Core.Models.DTOs.Common;
using Core.Models.Enums;
using Core.Validation;

namespace Core.Models.DTOs.Game
{
    public class GameSearchRequest : SearchRequestBase<GameFilterRequest>
    {
        [AllowedSortFields(
            nameof(Entities.Game.CreatedAt),
            nameof(Entities.Game.Id),
            nameof(Entities.Game.PlayerXId),
            nameof(Entities.Game.PlayerOId),
            nameof(Entities.Game.Status),
            nameof(Entities.Game.EndedAt)
        )]
        public override List<SortRequest> Sort
        {
            get => base.Sort;
            set => base.Sort = value;
        }
    }
}