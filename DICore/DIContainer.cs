using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DICore
{
    public class DIContainer
    {
        public static DIContainer GetInstance()
        {
            if (_instance is null)
                _instance = new DIContainer();

            return _instance;
        }
         
        private readonly List<ServiceDescriptor> _serviceDescriptors = new();
        private static DIContainer _instance;

        // Registra um serviço
        public void Register<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            _serviceDescriptors.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        }

        // Resolve a dependência
        public TService GetService<TService>() => (TService)GetService(typeof(TService));

        private object GetService(Type serviceType)
        {
            var descriptor = _serviceDescriptors.SingleOrDefault(d => d.ServiceType == serviceType);

            if (descriptor == null)
                throw new Exception($"Serviço do tipo {serviceType.Name} não foi registrado.");

            // Se for Singleton e já tiver instância, retorna ela
            if (descriptor.Implementation != null)
                return descriptor.Implementation;

            // Pega o primeiro construtor da implementação
            var actualType = descriptor.ImplementationType;
            var constructor = actualType.GetConstructors().First();

            // Resolve recursivamente os parâmetros do construtor
            var parameters = constructor.GetParameters()
                .Select(p => GetService(p.ParameterType))
                .ToArray();

            var implementation = Activator.CreateInstance(actualType, parameters);

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
                descriptor.Implementation = implementation;

            return implementation;
        }
    }
}
