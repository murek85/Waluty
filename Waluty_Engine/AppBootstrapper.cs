using Caliburn.Micro;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Waluty.Engine.Interfaces;
using Waluty.Engine.AppBoot;

namespace Waluty.Engine
{
    public class AppBootstrapper : Bootstrapper<IMain>
    {
        private CompositionContainer _container;
        private IWindowManager _windowManager;
        private EventAggregator _eventAggregator;

        public AppBootstrapper()
        {
            Start();
        }

        protected override void Configure()
        {
            var config = new TypeMappingConfiguration
            {
                DefaultSubNamespaceForViews = "Waluty.Views",
                DefaultSubNamespaceForViewModels = "Waluty.Engine.ViewModels"
            };
            Caliburn.Micro.ViewLocator.ConfigureTypeMappings(config);
            Caliburn.Micro.ViewModelLocator.ConfigureTypeMappings(config);

            var catalog = new AggregateCatalog(
                    AssemblySource.Instance.Select(x => new AssemblyCatalog(x)).OfType<ComposablePartCatalog>());

            _container = new CompositionContainer(catalog);
            _windowManager = new WindowManager();
            _eventAggregator = new EventAggregator();

            var batch = new CompositionBatch();

            batch.AddExportedValue<IWindowManager>(_windowManager);
            batch.AddExportedValue<IEventAggregator>(_eventAggregator);
            batch.AddExportedValue(_container);
            batch.AddExportedValue(catalog);

            _container.Compose(batch);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetExportedValues<object>(AttributedModelServices.GetContractName(service));
        }

        protected override object GetInstance(Type service, string key)
        {
            var contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(service) : key;
            var exports = _container.GetExportedValues<object>(contract);

            if (exports.Any())
            {
                return exports.First();
            }

            throw new ArgumentException(String.Format("Nie można zlokalizować żadnej instancji!"));
        }

        protected override void BuildUp(object instance)
        {
            _container.SatisfyImportsOnce(instance);
        }

        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            var assemblies = new List<Assembly>
            {
                Assembly.GetExecutingAssembly()
            };
            var referencedAssemblies = assemblies[0].GetReferencedAssemblies();
            assemblies.AddRange(referencedAssemblies.Select(Assembly.Load));
            assemblies.AddRange(base.SelectAssemblies());

            return assemblies;
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            var startupTasks = GetAllInstances(typeof(StartupTask))
                .Cast<ExportedDelegate>()
                .Select(exportedDelegate => (StartupTask)exportedDelegate.CreateDelegate(typeof(StartupTask)));
            startupTasks.Apply(s => s());

            base.OnStartup(sender, e);
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            base.OnExit(sender, e);
        }

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            base.OnUnhandledException(sender, e);

            e.Handled = true;
            Application.Shutdown();
        }
    }
}
