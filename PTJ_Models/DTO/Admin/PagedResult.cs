using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    
    
    public class PagedResult<T>
    {
       
        public IEnumerable<T> Data { get; }

        public int TotalRecords { get; }

        public int Page { get; }

        public int PageSize { get; }

        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        public bool HasNext => Page < TotalPages;

        public bool HasPrevious => Page > 1;

        public PagedResult(IEnumerable<T> data, int totalRecords, int page, int pageSize)
        {
            Data = data ?? Array.Empty<T>();
            TotalRecords = totalRecords;
            Page = page;
            PageSize = pageSize;
        }
    }
}
