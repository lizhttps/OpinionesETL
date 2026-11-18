using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Extractors.Interfaces;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Extractors
{
    public class DatabaseExtractor : IExtractor<WebReview>
    {
        private readonly CustomerReviewsDBContext _context;

        public DatabaseExtractor(CustomerReviewsDBContext context)
        {
            _context = context;
        }

        public async Task<List<WebReview>> ExtractAsync(CancellationToken cancellationToken = default)
        {
            return await _context.WebReviews
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}