using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using SqlAzureBackup.Worker.Jobs;
using System.Threading.Tasks;
using SqlAzureBackup.Worker.Scheduler;

namespace SqlAzureBackup.Worker
{
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

            Trace.WriteLine("Setting up jobs");
            
            var jobScheduler = new JobScheduler(new TimeSpan(Int32.Parse(RoleEnvironment.GetConfigurationSettingValue("backupFrequencyInHours")), 0, 0));
            jobScheduler.AddJob(new ExportBacpacJob());
            jobScheduler.AddJob(new CheckLastBackupStatusJob());
            jobScheduler.AddJob(new SendFileViaFTP());

            Trace.WriteLine("Starting the WorkerRole loop");
            while (true)
            {
                jobScheduler.TryExecuteJobs();
                Thread.Sleep(1000);
            }
        }
    }
}