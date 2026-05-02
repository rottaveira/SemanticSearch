using DICore;
using Domain.Index.Impl;
using Domain.Index.Interfaces;
using Domain.Search.Impl;
using Domain.Search.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Configuration
    {
        public void Configure()
        {
            var container = DIContainer.GetInstance();
            container.Register<IIndexingManager, IndexingManager>(ServiceLifetime.Singleton);
            container.Register<ISearchManager, SearchManager>(ServiceLifetime.Singleton);
        }
    }
}
