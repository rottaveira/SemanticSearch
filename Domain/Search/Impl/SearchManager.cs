using Domain.Search.Interfaces;
using Infrastructure.Index.Interface;
using Payload.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Search.Impl
{
    public class SearchManager: ISearchManager
    {
        private readonly ILuceneEngine _luceneEngine;

        public SearchManager(ILuceneEngine luceneEngine)
        {
            _luceneEngine = luceneEngine;
        }

        public List<DocumentModel> GetAll() => _luceneEngine.GetAllDocuments();
    }
}
