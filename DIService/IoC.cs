using DICore;

namespace DIService
{
    public class IoC
    {
        public void Initialize()
        {
            new Domain.Configuration().Configure();
            new Infrastructure.Configuration().Configure(); 
            new InferenceCore.Configuration().Configure(); 
        }

        public TService GetService<TService>() => DIContainer.GetInstance().GetService<TService>();
    }
}
