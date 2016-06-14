using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace Waluty.Engine.AppBoot
{
    [Export(typeof(IServiceLocator))]
    public class ServiceLocator : IServiceLocator
    {
        private readonly CompositionContainer compositionContainer;
        
        [ImportingConstructor]
        public ServiceLocator(CompositionContainer compositionContainer)
        {
            this.compositionContainer = compositionContainer;
        }
        
        public T GetInstance<T>() where T : class
        {
            var instance = this.compositionContainer.GetExportedValue<T>();
            if (instance != null)
            {
                return instance;
            }

            throw new Exception(string.Format("Nie można zlokalizować instancji {0}.", typeof(T)));
        }
    }
}
