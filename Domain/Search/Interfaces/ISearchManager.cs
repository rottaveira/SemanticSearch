using Payload.Document;

namespace Domain.Search.Interfaces
{
    public interface ISearchManager
    {
        List<DocumentModel> GetAll();
    }
}