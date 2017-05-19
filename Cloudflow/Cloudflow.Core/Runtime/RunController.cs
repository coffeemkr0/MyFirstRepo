﻿using Cloudflow.Core.Data.Agent;
using Cloudflow.Core.Data.Agent.Models;
using Cloudflow.Core.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudflow.Core.Runtime
{
    public class RunController
    {
        #region Events
        public event RunStatusChangedEventHandler RunStatusChanged;
        protected virtual void OnRunStatusChanged()
        {
            RunStatusChangedEventHandler temp = RunStatusChanged;
            if (temp != null)
            {
                temp(this.Run);
            }
        }

        public event RunOutputEventHandler RunOutput;
        protected virtual void OnRunOutput(OutputEventLevels level, string message)
        {
            RunOutputEventHandler temp = RunOutput;
            if (temp != null)
            {
                temp(this.Run, level, message);
            }
        }

        public event StepOutputEventHandler StepOutput;
        protected virtual void OnStepOutput(Step step, OutputEventLevels level, string message)
        {
            StepOutputEventHandler temp = StepOutput;
            if (temp != null)
            {
                temp(step, level, message);
            }
        }
        #endregion

        #region Properties
        public string Name { get; }

        public IJob Job { get; }

        public Dictionary<string, object> Triggerdata { get; }

        public log4net.ILog RunLogger { get; }

        public Run Run { get; set; }

        public AgentDbContext AgentDbContext { get; set; }
        #endregion

        #region Constructors
        public RunController(string name, IJob job, Dictionary<string, object> triggerData)
        {
            this.RunLogger = log4net.LogManager.GetLogger("RunController." + name);

            this.Name = name;
            this.Job = job;
            this.Triggerdata = triggerData;

            this.AgentDbContext = new AgentDbContext();

            this.Run = new Run
            {
                Name = this.Name,
                JobName = this.Job.Name,
                DateQueued = DateTime.Now,
                Status = Run.RunStatuses.Queued
            };
            this.AgentDbContext.Runs.Add(this.Run);
            this.AgentDbContext.SaveChanges();

            OnRunStatusChanged();
        }
        #endregion

        #region Private Methods
        private void ExecuteSteps()
        {
            foreach (var step in this.Job.Steps)
            {
                this.RunLogger.Info(string.Format("Begin step {0}", step.Name));

                try
                {
                    OnRunOutput(OutputEventLevels.Info, $"Execute step {step.Name}");

                    StepController stepController = new StepController(step, this.Triggerdata);
                    stepController.StepOutput += StepController_StepOutput;
                    stepController.ExecuteStep();

                    this.RunLogger.Info(string.Format("End step {0}", step.Name));
                }
                catch (Exception ex)
                {
                    this.RunLogger.Error(ex);
                    OnRunOutput(OutputEventLevels.Error, ex.ToString());
                }
            }
        }

        private void StepController_StepOutput(Step step, OutputEventLevels level, string message)
        {
            OnStepOutput(step, level, message);
        }
        #endregion

        #region Public Methods
        public void ExecuteRun()
        {
            this.RunLogger.Info(string.Format("Starting run {0}", this.Name));

            this.Run.DateStarted = DateTime.Now;
            this.Run.Status = Run.RunStatuses.Running;
            this.AgentDbContext.SaveChanges();
            OnRunStatusChanged();

            try
            {
                ExecuteSteps();
                this.Run.Status = Run.RunStatuses.Completed;
            }
            catch (Exception ex)
            {
                this.Run.Status = Run.RunStatuses.Failed;
                this.RunLogger.Error(ex);
            }

            this.Run.DateEnded = DateTime.Now;
            this.AgentDbContext.SaveChanges();
            this.AgentDbContext.Dispose();
            OnRunStatusChanged();
        }
        #endregion
    }
}
