﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using CommandLine;
using Deployer;
using Deployer.Lumia;
using Deployer.Lumia.NetFx;
using Deployer.Tasks;
using Deployment.Console.Options;
using Grace.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Deployment.Console
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            ConfigureLogger();

            try
            {
                var container = new DependencyInjectionContainer();

                var installOptionsProvider = new WindowsDeploymentOptionsProvider();
                container.Configure(x =>
                {
                    DeployerComposition.Configure(x);
                    x.Export<ConsoleMarkdownDisplayer>().As<IMarkdownDisplayer>();
                    x.ExportFactory(() => installOptionsProvider).As<IWindowsOptionsProvider>();
                });
                var deployer = container.Locate<IAutoDeployer>();

                await Parser.Default
                    .ParseArguments<WindowsDeploymentCmdOptions, 
                        EnableDualBootCmdOptions, 
                        DisableDualBootCmdOptions,
                        InstallGpuCmdOptions,
                        NonWindowsDeploymentCmdOptions>
                        (args)
                    .MapResult(
                        (WindowsDeploymentCmdOptions opts) =>
                        {
                            installOptionsProvider.Options = new InstallOptions()
                            {
                                ImageIndex = opts.Index,
                                ImagePath = opts.WimImage,
                                SizeReservedForWindows = ByteSize.FromGigaBytes(opts.ReservedSizeForWindowsInGb),
                            };
                            return deployer.Deploy();
                        },
                        (EnableDualBootCmdOptions opts) => deployer.ToogleDualBoot(true),
                        (DisableDualBootCmdOptions opts) => deployer.ToogleDualBoot(false),
                        (InstallGpuCmdOptions opts) => deployer.InstallGpu(),
                        (NonWindowsDeploymentCmdOptions opts) => deployer.Deploy(),
                        HandleErrors);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Operation failed");
                throw;
            }
        }       

        private static Task HandleErrors(IEnumerable<Error> errs)
        {
            System.Console.WriteLine($"Invalid command line: {string.Join("\n", errs.Select(x => x.Tag))}");
            return Task.CompletedTask;
        }

        private static void ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Information)
                .WriteTo.RollingFile(@"Logs\Log-{Date}.txt")
                .MinimumLevel.Verbose()
                .CreateLogger();
        }
    }
}