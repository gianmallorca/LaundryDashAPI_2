

using LaundryDashAPI_2.DTOs;

namespace LaundryDashAPI_2.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> Paginate<T>(this IQueryable<T> queryuable, PaginationDTO paginationDTO)
        {
            return queryuable.Skip((paginationDTO.Page - 1) * paginationDTO.RecordsPerPage)
                .Take(paginationDTO.RecordsPerPage);
        }
    }
}
