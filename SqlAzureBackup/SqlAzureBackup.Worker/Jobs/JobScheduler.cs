namespace SqlAzureBackup.Worker.Jobs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    public class JobScheduler<T> where T : IJobContext
    {
        public string Name { get; set; }

        public TimeSpan Frequency { get; set; }
        public DateTime NextExecutionTime { get; set; }

        private T JobContext { get; set; }
        private List<IJob<T>> Jobs { get; set; }

        public JobScheduler(string blobNextExecutionTimeName, T jobContext, TimeSpan? frequency = null, DateTime? nextExecutionTime = null)
        {
            this.Name = (!string.IsNullOrEmpty(blobNextExecutionTimeName)) ? blobNextExecutionTimeName : string.Format("job-scheduler/next-execution-time-{0}.txt", Guid.NewGuid().ToString());
            this.JobContext = jobContext;
            this.Jobs = new List<IJob<T>>();
            this.Frequency = (frequency.HasValue) ? frequency.Value : TimeSpan.FromHours(24);
            this.NextExecutionTime = this.GetNextExecutionTime();
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
                this.SaveNextExecutionTime(this.NextExecutionTime);
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

        private DateTime GetNextExecutionTime()
        {
            DateTime toReturn = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, AzureHelper.BackupExecutionHour, 0, 0);
            string containerName = this.Name.Contains('/') ? this.Name.Substring(0, this.Name.IndexOf('/')) : string.Empty;
            string blobName = this.Name.Contains('/') ? this.Name.Substring(this.Name.IndexOf('/') + 1) : this.Name;
            string dateString = AzureHelper.GetTextBlob(AzureHelper.StorageConnectionString, containerName, blobName);

            if (string.IsNullOrEmpty(dateString))
            {
                this.SaveNextExecutionTime(toReturn);
            }
            else
            {
                toReturn = DateTime.Parse(dateString, CultureInfo.CreateSpecificCulture("en-US"));
            }

            return toReturn;
        }

        private void SaveNextExecutionTime(DateTime executionTime)
        {
            string containerName = this.Name.Contains('/') ? this.Name.Substring(0, this.Name.IndexOf('/')) : string.Empty;
            string blobName = this.Name.Contains('/') ? this.Name.Substring(this.Name.IndexOf('/') + 1) : this.Name;
            AzureHelper.SaveTextToBlob(AzureHelper.StorageConnectionString, containerName, blobName, executionTime.ToString("MM-dd-yyyy HH:mm"));
        }
    }
}