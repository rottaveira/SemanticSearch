using Domain.Index.Interfaces;
using Infrastructure.Schemas;
using Payload.Document;
using Infrastructure.Lucene.Index.Interface;

namespace Domain.Index.Impl
{
    public class IndexingManager : IIndexingManager
    {
        private readonly ILuceneEngine _luceneEngine;

        public IndexingManager(ILuceneEngine engine)
        {
           _luceneEngine = engine;
        }

        public void InitializeIndex(SchemaConfig schemaConfig)
        {
            _luceneEngine.InitializeLucene(schemaConfig);
        }

        public void ReIndexAll(SchemaConfig mySchema)
        {
            _luceneEngine.ReindexAll(mySchema);
        }

        public void InsertDocument(DocumentModel document)
        {
            _luceneEngine.IndexDocument(new List<DocumentModel> { document });
        }
    }
}
