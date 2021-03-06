﻿using Cloudflow.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Cloudflow.Core;
using Cloudflow.Core.ExtensionManagement;
using Cloudflow.Core.Serialization;
using Cloudflow.Web.Properties;

namespace Cloudflow.Web.ViewModels.Jobs
{
    public class ExtensionBrowserViewModel
    {
        #region Properties
        public string Id { get; set; }

        public string Caption { get; set; }

        public List<ExtensionLibrary> ExtensionLibraries { get; set; }
        #endregion

        #region Constructors
        public ExtensionBrowserViewModel()
        {
            ExtensionLibraries = new List<ExtensionLibrary>();
        }
        #endregion

        #region Public Methods
        public static ExtensionBrowserViewModel GetModel(string id, string extensionLibraryFolder, ConfigurableExtensionTypes extensionType)
        {
            var model = new ExtensionBrowserViewModel
            {
                Id = id,
            };

            switch (extensionType)
            {
                case ConfigurableExtensionTypes.Trigger:
                    model.Caption = "Triggers";
                    break;
                case ConfigurableExtensionTypes.Step:
                    model.Caption = "Steps";
                    break;
                case ConfigurableExtensionTypes.Condition:
                    model.Caption = "Conditions";
                    break;
            }

            foreach (var extensionLibraryFile in Directory.GetFiles(extensionLibraryFolder, "*.dll"))
            {
                var extensionLibrary = new ExtensionLibrary
                {
                    Caption = FileVersionInfo.GetVersionInfo(extensionLibraryFile).ProductName
                };

                model.ExtensionLibraries.Add(extensionLibrary);

                var extensionService = new ExtensionService(new JsonConfigurationSerializer());
                var assemblyCatalogProvider = new AssemblyCatalogProvider(extensionLibraryFile);

                switch (extensionType)
                {
                    case ConfigurableExtensionTypes.Trigger:
                        foreach (var triggerDescriptor in extensionService.GetTriggerDescriptors(assemblyCatalogProvider))
                        {
                            var extension = new ExtensionLibrary.Extension
                            {
                                ExtensionId = triggerDescriptor.ExtensionId,
                                ExtensionAssemblyPath = extensionLibraryFile,
                                Name = triggerDescriptor.Name,
                                Description = triggerDescriptor.Description,
                                Icon = triggerDescriptor.Icon ?? Resources.GenericExtensionIcon
                            };

                            extensionLibrary.Extensions.Add(extension);
                        }
                        break;
                    case ConfigurableExtensionTypes.Step:
                        foreach (var stepDescriptor in extensionService.GetStepDescriptors(assemblyCatalogProvider))
                        {
                            var extension = new ExtensionLibrary.Extension
                            {
                                ExtensionId = stepDescriptor.ExtensionId,
                                ExtensionAssemblyPath = extensionLibraryFile,
                                Name = stepDescriptor.Name,
                                Description = stepDescriptor.Description,
                                Icon = stepDescriptor.Icon ?? Resources.GenericExtensionIcon
                            };

                            extensionLibrary.Extensions.Add(extension);
                        }
                        break;
                    case ConfigurableExtensionTypes.Condition:
                        foreach (var conditionDescriptor in extensionService.GetConditionDescriptors(assemblyCatalogProvider))
                        {
                            var extension = new ExtensionLibrary.Extension
                            {
                                ExtensionId = conditionDescriptor.ExtensionId,
                                ExtensionAssemblyPath = extensionLibraryFile,
                                Name = conditionDescriptor.Name,
                                Description = conditionDescriptor.Description,
                                Icon = conditionDescriptor.Icon ?? Resources.GenericExtensionIcon
                            };

                            extensionLibrary.Extensions.Add(extension);
                        }
                        break;
                }
                
            }

            return model;
        }
        #endregion

        #region ExtensionLibrary
        public class ExtensionLibrary
        {
            #region Properties
            public string Caption { get; set; }

            public List<Extension> Extensions { get; set; }
            #endregion

            #region Constructors
            public ExtensionLibrary()
            {
                Extensions = new List<Extension>();
            }
            #endregion

            #region Extension
            public class Extension
            {
                #region Properties
                public Guid ExtensionId { get; set; }

                public string ExtensionAssemblyPath { get; set; }

                public string Name { get; set; }

                public string Description { get; set; }

                public byte[] IconArray
                {
                    get
                    {
                        using (var ms = new MemoryStream())
                        {
                            Icon.Save(ms, Icon.RawFormat);
                            return ms.ToArray();
                        }
                    }
                }

                public Image Icon { get; set; }
                #endregion
            }
            #endregion
        }
        #endregion
    }
}