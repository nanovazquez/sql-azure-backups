using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using SqlAzureBackup.Worker.Jobs.Interfaces;
using System;
using System.Text;

namespace SqlAzureBackup.Worker.Jobs
{
    // sample execution command line
    // DacIESvcCli -STATUS -S myazure -U myuser -P p@ssword [-R myreqId]

    public class CheckLastBackupStatusJob : CommandLineJob
    {
        public StringBuilder LogData { get; set; }

        public CheckLastBackupStatusJob()
        {
            this.LogData = new StringBuilder();
            this.ProcessName = "DacIESvcCli.exe";
            this.TraceInfoMessage = "Checking last backup status...";
            this.Arguments = string.Empty;
            
            // server arguments (url, user login, user pass)
            this.Arguments += string.Format(" -s {0} ", AzureHelper.ServerName);
            this.Arguments += string.Format(" -u {0} ", AzureHelper.SqlAzureUsername);
            this.Arguments += string.Format(" -p {0} ", AzureHelper.SqlAzurePassword);

            // operation type 
            this.Arguments += string.Format(" -STATUS ");
        }

        protected override void OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                this.LogData.AppendLine(e.Data.Replace(",",string.Empty).Trim());
            }
        }

        protected override void Exited(object sender, EventArgs e)
        {
            if (this.LogData.Length > 0)
            {
                // check if the last backup was completed (either successfully or not)
                bool isCompleted = this.CheckIfLastBackupIsCompleted();

                // if completed, save the data in blob storage
                if (isCompleted)
                {
                    // save the log in the container
                    AzureHelper.SaveTextToBlob(AzureHelper.StorageConnectionString, AzureHelper.BackupContainerName, "logFile.txt", this.LogData.ToString());
                }
            }
        }

        private bool CheckIfLastBackupIsCompleted()
        {
            bool toReturn = false;
            string logData = this.LogData.ToString();

            // get latest backup info
            string backupInfo = logData.Substring(logData.LastIndexOf("Server:"));

            // get status
            string status = backupInfo.Substring(backupInfo.IndexOf("Status:"), backupInfo.IndexOf("Last Modified Time") - backupInfo.IndexOf("Status:")).Trim().ToLower();

            if (status.Contains("completed"))
            {
                toReturn = true;
            }

            if (status.Contains("failed"))
            {
                string errorInfo = backupInfo.Substring(backupInfo.IndexOf("Error:"), backupInfo.IndexOf("Status Retrieval Complete") - backupInfo.IndexOf("Error:")).Trim();
                Trace.WriteLine(string.Format("Backup failed\n{0}", errorInfo), "Error");
                toReturn = true;
            }           

            return toReturn;
        }
    }
}