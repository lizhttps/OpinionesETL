
namespace OpinionesETL.Data.Daos.Interfaces
{
    public interface ISurveyDao
    {

        Task ClearSurveysAsync();
        Task LoadSurveysAsync(List<Models.Survey> surveys);
    }
}
