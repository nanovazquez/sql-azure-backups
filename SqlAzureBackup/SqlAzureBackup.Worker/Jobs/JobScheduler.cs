﻿namespace SqlAzureBackup.Worker.Jobs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;

    public class JobScheduler<T> where T : IJobContext
    {
        public TimeSpan Frequency { get; set; }
        public DateTime NextExecutionTime { get; set; }

        private T JobContext { get; set; }
        private List<IJob<T>> Jobs { get; set; }

        public JobScheduler(T jobContext, TimeSpan? frequency = null, DateTime? nextExecutionTime = null)
        {
            this.JobContext = jobContext;
            this.Jobs = new List<IJob<T>>();
            this.Frequency = (frequency.HasValue) ? frequency.Value : TimeSpan.FromHours(24);
            this.NextExecutionTime = (nextExecutionTime.HasValue) ? nextExecutionTime.Value : new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 3, 0, 0);
        }

        public void AddJob(IJob<T> job)
        {
            job.Context = this.JobContext;
            this.Jobs.Add(job);
        }

        /// <summary>
        /// It executes the jobs in parallel if it's the time to do it, 
        /// by comparing the Last Execution Time and the frequency selected. 
        /// </summary>
        /// <returns>True if the jobs are being executed. False otherwise</returns>
        public bool TryExecuteJobs()
        {
            DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"));
            bool haveToExecuteJobs = currentTime > this.NextExecutionTime && currentTime.Subtract(this.NextExecutionTime) > this.Frequency;

            if (haveToExecuteJobs)
            {
                Trace.WriteLine(string.Format("Executing Jobs on {0}", currentTime.ToShortDateString()));
                this.ExecuteJobs();
                this.NextExecutionTime = this.NextExecutionTime.AddHours(this.Frequency.TotalHours);
            }

            return haveToExecuteJobs;
        }

        /// <summary>
        /// Execute the jobs sequentially. The execution code is done in parallel
        /// </summary>
        protected void ExecuteJobs()
        {
            // in parallel
            Parallel.Invoke(() =>
            {
                // for each job create a task, that will run synchronously
                this.Jobs.Select(job => new Task(() => { job.Run(); }))
                         .ToList()
                         .ForEach((Task task) => { task.RunSynchronously(); });
            });
        }
    }
}