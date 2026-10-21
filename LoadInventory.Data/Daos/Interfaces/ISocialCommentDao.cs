

namespace OpinionesETL.Data.Daos.Interfaces
{
    public interface ISocialCommentDao
    {

        Task ClearSocialCommentAsync();
        Task LoadSocialCommentsAsync(List<Models.SocialComment> socialComments);

    }
}
