﻿using Cloudflow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Cloudflow.Core.Data.Agent;
using Cloudflow.Core.Data.Agent.Models;
using Cloudflow.Core.Framework;
using Cloudflow.Core.Configuration;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace Cloudflow.Core.Runtime
{
    public class Agent
    {
        #region Private Members
        private int _runCounter = 1;
        private List<Task> _runTasks;

        private List<RunController> _runControllers;
        #endregion

        #region Events
        public delegate void StatusChangedEventHandler(AgentStatus status);
        public event StatusChangedEventHandler StatusChanged;
        protected virtual void OnStatusChanged()
        {
            StatusChangedEventHandler temp = StatusChanged;
            if (temp != null)
            {
                temp(this.AgentStatus);
            }
        }

        public event RunStatusChangedEventHandler RunStatusChanged;
        protected virtual void OnRunStatusChanged(Run run)
        {
            RunStatusChangedEventHandler temp = RunStatusChanged;
            if (temp != null)
            {
                temp(run);
            }
        }
        #endregion

        #region Properties
        public List<JobController> JobControllers { get; }

        public log4net.ILog AgentLogger { get; }

        private AgentStatus _agentStatus;
        public AgentStatus AgentStatus
        {
            get { return _agentStatus; }
            set
            {
                if (_agentStatus != value)
                {
                    _agentStatus = value;
                    OnStatusChanged();
                }
            }
        }
        #endregion

        #region Constructors
        public Agent()
        {
            this.AgentLogger = log4net.LogManager.GetLogger("Agent." + Environment.MachineName);
            this.JobControllers = new List<JobController>();
            _runTasks = new List<Task>();
            _runControllers = new List<RunController>();
            this.AgentStatus = new AgentStatus { Status = AgentStatus.AgentStatuses.NotRunning };
        }
        #endregion

        #region Private Methods
        private void Job_JobTriggerFired(Job job, Trigger trigger, Dictionary<string, object> triggerData)
        {
            this.AgentLogger.Info(string.Format("Trigger fired - Job:{0} Trigger:{1}", job.JobConfiguration.Name, trigger.TriggerConfiguration.Name));

            RunController runController = new RunController(string.Format("{0} Run {1}", job.JobConfiguration.Name, _runCounter++), job, triggerData);
            runController.RunStatusChanged += RunController_RunStatusChanged;

            var task = Task.Run(() =>
            {
                try
                {
                    runController.ExecuteRun();
                }
                catch (Exception ex)
                {
                    this.AgentLogger.Error(ex);
                }
            });

            _runTasks.Add(task);
            _runControllers.Add(runController);

            Task.Run(() =>
            {
                task.Wait();
                _runTasks.Remove(task);
                _runControllers.Remove(runController);
            });
        }

        private void RunController_RunStatusChanged(Run run)
        {
            OnRunStatusChanged(run);
        }
        #endregion

        #region Public Methods
        public void Start()
        {
            this.AgentLogger.Info("Starting agent");

            this.AgentStatus = new AgentStatus { Status = AgentStatus.AgentStatuses.Starting };

            foreach (var jobController in this.JobControllers)
            {
                jobController.Start();
            }

            this.AgentStatus = new AgentStatus { Status = AgentStatus.AgentStatuses.Running };
        }

        public void Stop()
        {
            this.AgentLogger.Info("Stopping agent");

            this.AgentStatus = new AgentStatus { Status = AgentStatus.AgentStatuses.Stopping };

            foreach (var jobController in this.JobControllers)
            {
                jobController.Stop();
            }

            this.AgentLogger.Info("Waiting for any runs in progress");
            Task.WaitAll(_runTasks.ToArray());

            this.AgentLogger.Info("Agent stopped");
            this.AgentStatus = new AgentStatus { Status = AgentStatus.AgentStatuses.NotRunning };
        }

        public List<Run> GetQueuedRuns()
        {
            lock (_runControllers)
            {
                return _runControllers.Select(i => i.Run).ToList();
            }
        }

        public static Agent CreateTestAgent()
        {
            Agent agent = new Agent();

            var jobConfiguration = DefaultJobConfiguration.CreateTestJobConfiguration();
            var jobController = new JobController(jobConfiguration);
            agent.JobControllers.Add(jobController);

            //var jobConfiguration2 = DefaultJobConfiguration.CreateTestJobConfiguration("Test Job 2");
            //agent.AddJob(new TestJob(jobConfiguration2));

            return agent;
        }
        #endregion
    }
}
