
namespace OpinionesETL.Data.Daos.Interfaces
{
    public interface IWebReviewDao
    {

        Task ClearWebReviewsAsync();
        Task LoadWebReviewsAsync(List<Models.WebReview> webReviews);
    }
}
