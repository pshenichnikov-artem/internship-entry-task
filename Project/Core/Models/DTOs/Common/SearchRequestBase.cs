using Core.Models.Enums;

namespace Core.Models.DTOs.Common
{
    public abstract class SearchRequestBase<TFilter>
    where TFilter : new()
    {
        private List<SortRequest>? _sort;
        private PaginationRequest? _pagination;
        private TFilter? _filter;

        public virtual List<SortRequest> Sort
        {
            get
            {
                if (_sort == null)
                    _sort = new List<SortRequest>
                {
                };
                return _sort;
            }
            set => _sort = value;
        }

        public PaginationRequest Pagination
        {
            get
            {
                if (_pagination == null)
                    _pagination = new PaginationRequest();
                return _pagination;
            }
            set => _pagination = value;
        }

        public TFilter Filter
        {
            get
            {
                if (_filter == null)
                    _filter = new TFilter();
                return _filter;
            }
            set => _filter = value;
        }
    }
}
