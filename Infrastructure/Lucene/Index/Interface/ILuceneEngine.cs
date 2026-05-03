using Infrastructure.Schemas;
using Payload.Document;

namespace Infrastructure.Lucene.Index.Interface
{
    public interface ILuceneEngine
    {
        void DeleteDocument(string idFieldName, string idValue);
        List<DocumentModel> GetAllDocuments();
        void IndexDocument(IEnumerable<DocumentModel> documents); 
        void InitializeLucene(SchemaConfig mySchema);
        void ReindexAll(SchemaConfig mySchema);
        void UpdateDocument(string idFieldName, string idValue, DocumentModel updatedDoc);
    }
}
