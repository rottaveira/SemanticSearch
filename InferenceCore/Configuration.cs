using DICore;
using InferenceCore.NER.Impl;
using InferenceCore.NER.Interfaces;
using InferenceCore.ONNX.Impl;
using InferenceCore.ONNX.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace InferenceCore
{
    public class Configuration
    {
        public void Configure()
        {
            var container = DIContainer.GetInstance();
            container.Register<IOnnxExecutor, OnnxExecutor>(ServiceLifetime.Singleton); 
            container.Register<IOnnxInferenceEngine, OnnxInferenceEngine>(ServiceLifetime.Singleton); 
            container.Register<INerPostProcessor, NerPostProcessor>(ServiceLifetime.Singleton); 
        }
    }
}
