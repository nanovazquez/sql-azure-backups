namespace SqlAzureBackup.Worker
{
    using System;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Net;
    using System.Threading;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using SqlAzureBackup.Worker.Jobs;
    using SqlAzureBackup.Worker.Jobs.AzureBackupJobs;

    public class WorkerRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();
            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);


            return base.OnStart();
        }

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("SqlAzureBackup.Worker entry point called", "Information");

            Trace.WriteLine("Setting up jobs", "Info");

            var jobScheduler = new JobScheduler<SqlAzureBackupJobContext>(
                                                jobContext: new SqlAzureBackupJobContext(),
                                                frequency: new TimeSpan(AzureHelper.BackupFrequencyInHours, 0, 0),
                                                nextExecutionTime: new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, AzureHelper.BackupExecutionHour, 0, 0));
            
            jobScheduler.AddJob(new ResolveDatacenterJob());
            jobScheduler.AddJob(new ExportBacpacJob());
            jobScheduler.AddJob(new CheckBacpacStatusJob());
            jobScheduler.AddJob(new SendFileViaFTP());

            Trace.WriteLine("Starting the WorkerRole loop", "Info");
            while (true)
            {
                jobScheduler.TryExecuteJobs();
                Thread.Sleep(1000);
            }
        }
    }
}
