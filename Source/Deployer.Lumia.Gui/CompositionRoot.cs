using System;
using Deployer.Gui;
using Deployer.Gui.Services;
using Deployer.Gui.ViewModels;
using Deployer.Lumia.Gui.Specifics;
using Deployer.Lumia.Gui.ViewModels;
using Deployer.Lumia.NetFx;
using Deployer.NetFx;
using Deployer.Tasks;
using Grace.DependencyInjection;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace Deployer.Lumia.Gui
{
    public static class CompositionRoot
    {
        public static DependencyInjectionContainer CreateContainer()
        {
            var container = new DependencyInjectionContainer();

            IObservable<LogEvent> logEvents = null;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile(@"Logs\Log-{Date}.txt")
                .WriteTo.Observers(x => logEvents = x)
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Verbose($"Started {AppProperties.AppTitle}");

            container.Configure(x =>
            {
                x.Configure();
                x.ExportFactory(() => new OperationProgress()).As<IOperationProgress>().Lifestyle.Singleton();
                x.ExportFactory(() => logEvents).As<IObservable<LogEvent>>().Lifestyle.Singleton();
                x.Export<WimPickViewModel>().As<WimPickViewModel>().Lifestyle.Singleton();
                x.Export<UIServices>().Lifestyle.Singleton();
                x.Export<Dialog>().ByInterfaces().Lifestyle.Singleton();
                x.Export<OpenFilePicker>().As<IOpenFilePicker>().Lifestyle.Singleton();
                x.Export<SaveFilePicker>().As<ISaveFilePicker>().Lifestyle.Singleton();
                x.Export<LumiaSettingsService>().ByInterfaces().Lifestyle.Singleton();
                x.Export<SaveFilePicker>().As<ISaveFilePicker>().Lifestyle.Singleton();
                x.Export<ViewService>().As<IViewService>().Lifestyle.Singleton();
                x.ExportFactory(() => DialogCoordinator.Instance).As<IDialogCoordinator>().Lifestyle.Singleton();     
                x.ExportAssemblies(Assemblies.AppDomainAssemblies)
                    .Where(y => typeof(ISection).IsAssignableFrom(y))
                    .ByInterface<ISection>()
                    .ByType()
                    .ExportAttributedTypes()
                    .Lifestyle.Singleton();
            });

            return container;
        }
    }
}