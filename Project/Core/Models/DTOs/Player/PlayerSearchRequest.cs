using Core.Models.DTOs.Common;
using Core.Models.DTOs.Move;
using Core.Models.Enums;
using Core.Validation;

namespace Core.Models.DTOs.Player
{
    public class PlayerSearchRequest : SearchRequestBase<PlayerFilterRequest>
    {
        [AllowedSortFields(
            nameof(Entities.Player.Username),
            nameof(Entities.Player.Id)
        )]
        public override List<SortRequest> Sort
        {
            get => base.Sort;
            set => base.Sort = value;
        }
    }
}