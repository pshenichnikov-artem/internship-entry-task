using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;
using Core.Models.Enums;
using Core.Validation;

namespace Core.Models.DTOs.Move
{
    public class MoveSearchRequest : SearchRequestBase<MoveFilterRequest>
    {
        [AllowedSortFields(
            nameof(Entities.Move.Id),
            nameof(Entities.Move.GameId),
            nameof(Entities.Move.MoveNumber),
            nameof(Entities.Move.PlayerId),
            nameof(Entities.Move.ClientMoveId),
            nameof(Entities.Move.CreatedAt)
        )]
        public override List<SortRequest> Sort
        {
            get => base.Sort;
            set => base.Sort = value;
        }
    }
}