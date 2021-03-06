﻿using Cloudflow.Core.Extensions;
using System;
using System.IO;

namespace TestApp
{
    class Program
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            try
            {
                var extensionAssemblyPath = Path.GetFullPath(@"..\..\..\Cloudflow.Extensions\bin\debug\Cloudflow.Extensions.dll");
                var extensionBrowser = new ConfigurableExtensionBrowser(extensionAssemblyPath);

                Console.WriteLine("Jobs");
                foreach (var triggerMetaData in extensionBrowser.GetConfigurableExtensions(ConfigurableExtensionTypes.Job))
                {
                    Console.WriteLine(triggerMetaData.ExtensionType);
                }

                Console.WriteLine("Triggers");
                foreach (var triggerMetaData in extensionBrowser.GetConfigurableExtensions(ConfigurableExtensionTypes.Trigger))
                {
                    Console.WriteLine(triggerMetaData.ExtensionType);
                }

                Console.WriteLine("Steps");
                foreach (var triggerMetaData in extensionBrowser.GetConfigurableExtensions(ConfigurableExtensionTypes.Step))
                {
                    Console.WriteLine(triggerMetaData.ExtensionType);
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex);
            }

            Console.ReadLine();
        }
    }
}
