using Infrastructure.Schemas;
using Payload.Document;

namespace Domain.Index.Interfaces
{
    public interface IIndexingManager
    {
        void InitializeIndex(SchemaConfig schemaConfig);
        void InsertDocument(DocumentModel document);
        void ReIndexAll(SchemaConfig mySchema);
    }
}