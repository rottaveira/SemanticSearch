using DICore;
using Infrastructure.Index.Impl;
using Infrastructure.Index.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public class Configuration
    {
        public void Configure()
        {
            var container = DIContainer.GetInstance();
            container.Register<ILuceneEngine, LuceneEngine>(ServiceLifetime.Singleton); 
        }
    }
}
