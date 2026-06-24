using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Wrappers
{
    public class PageResult<T>
    {
        public IEnumerable<T> Items { get; private set; }
        public int TotalItems { get; private set; }
        public int PageNumber { get; private set; }
        public int PageSize { get; private set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public PageResult(IEnumerable<T> items, int totalItems, int pageNumber, int pageSize)
        {
            Items = items;
            TotalItems = totalItems;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
