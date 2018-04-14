﻿using Cloudflow.Core.Data.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace Cloudflow.Core.Extensions.Controllers
{
    public class TriggerController
    {
        #region Events
        public delegate void TriggerFiredEventHandler(Trigger trigger);
        public event TriggerFiredEventHandler TriggerFired;
        protected virtual void OnTriggerFired(Trigger trigger)
        {
            var temp = TriggerFired;
            if (temp != null)
            {
                temp(trigger);
            }
        }
        #endregion

        #region Private Members
        private CompositionContainer _triggersContainer;
        [ImportMany]
        IEnumerable<Lazy<IConfigurableExtension, IConfigurableExtensionMetaData>> _extensions = null;
        #endregion

        #region Properties
        public TriggerDefinition TriggerDefinition { get; }

        public ExtensionConfiguration TriggerConfiguration { get; }

        public List<ConditionController> ConditionControllers { get; }

        public log4net.ILog TriggerControllerLoger { get; }

        public Trigger Trigger { get; }
        #endregion

        #region Constructors
        public TriggerController(TriggerDefinition triggerDefinition)
        {
            TriggerDefinition = triggerDefinition;
            TriggerControllerLoger = log4net.LogManager.GetLogger($"TriggerController.{triggerDefinition.TriggerDefinitionId}");

            var triggerConfigurationController = new ExtensionConfigurationController(triggerDefinition.ConfigurationExtensionId,
                triggerDefinition.ConfigurationExtensionAssemblyPath);
            TriggerConfiguration = triggerConfigurationController.Load(triggerDefinition.Configuration);

            ConditionControllers = new List<ConditionController>();
            foreach (var triggerConditionDefinition in triggerDefinition.TriggerConditionDefinitions)
            {
                var conditionController = new ConditionController(triggerConditionDefinition.TriggerConditionDefinitionId,
                    triggerConditionDefinition.ExtensionId, triggerConditionDefinition.ExtensionAssemblyPath,
                    triggerConditionDefinition.ConfigurationExtensionId, triggerConditionDefinition.ConfigurationExtensionAssemblyPath,
                    triggerConditionDefinition.Configuration);

                ConditionControllers.Add(conditionController);
            }

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(triggerDefinition.ExtensionAssemblyPath));
            _triggersContainer = new CompositionContainer(catalog);
            _triggersContainer.ComposeExportedValue<ExtensionConfiguration>("ExtensionConfiguration", TriggerConfiguration);

            try
            {
                _triggersContainer.ComposeParts(this);

                foreach (var i in _extensions)
                {
                    if (Guid.Parse(i.Metadata.ExtensionId) == triggerDefinition.ExtensionId)
                    {
                        Trigger = (Trigger)i.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                TriggerControllerLoger.Error(ex);
            }
        }
        #endregion

        #region Private Methods
        private void Trigger_Fired(Trigger trigger)
        {
            //Do not raise the TriggerFired event if any condition is not met
            foreach (var conditionController in ConditionControllers)
            {
                if (!conditionController.CheckCondition()) return;
            }

            OnTriggerFired(trigger);
        }
        #endregion

        #region Public Methods
        public void Start()
        {
            Trigger.Fired += Trigger_Fired;
            Trigger.Start();
        }

        public void Stop()
        {
            Trigger.Fired -= Trigger_Fired;
            Trigger.Stop();
        }
        #endregion
    }
}
