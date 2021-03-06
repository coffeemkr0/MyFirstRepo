﻿using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace TempProject.ExtensionService
{
    public class AssemblyCatalogProvider : ICatalogProvider
    {
        private readonly string _assemblyPath;

        public AssemblyCatalogProvider(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        public ComposablePartCatalog GetCatalog()
        {
            return new AssemblyCatalog(_assemblyPath);
        }
    }
}