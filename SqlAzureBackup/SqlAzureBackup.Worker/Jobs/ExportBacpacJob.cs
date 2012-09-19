using System;
using Microsoft.WindowsAzure.ServiceRuntime;
using SqlAzureBackup.Worker.Jobs.Interfaces;

namespace SqlAzureBackup.Worker.Jobs
{
    // sample execution command line
    // DacIESvcCli.exe -s [serverName].database.windows.net -d [databaseName] -u [user] -p [pass]
    // -bloburl http://[blobAccountName].blob.core.windows.net/[blobContainerName]/[bacpacFilename].bacpac 
    // -blobaccesskey [key] -accesskeytype storage -x

    public class ExportBacpacJob : CommandLineJob
    {
        public ExportBacpacJob()
        {
            this.ProcessName = "DacIESvcCli.exe";
            this.Arguments = string.Empty;
            this.TraceInfoMessage = string.Format("Starting backup process of {0}", DateTime.Now.ToString("MM-dd-yyyy"));
            string bacpacBlobUri = string.Format("{0}/{1}/backup-{2}.bacpac", AzureHelper.BlobUrl, AzureHelper.BackupContainerName,
                                                  DateTime.Now.ToString("MM-dd-yyyy"));

            // server arguments
            this.Arguments += string.Format(" -s {0} ", AzureHelper.ServerName);
            this.Arguments += string.Format(" -d {0} ", AzureHelper.DatabaseName);
            this.Arguments += string.Format(" -u {0} ", AzureHelper.SqlAzureUsername);
            this.Arguments += string.Format(" -p {0} ", AzureHelper.SqlAzurePassword);

            // blob arguments
            this.Arguments += string.Format("-bloburl {0} ", bacpacBlobUri);
            this.Arguments += string.Format("-blobaccesskey {0} ", AzureHelper.BlobAccessKey);
            this.Arguments += string.Format("-accesskeytype storage");

            // operation type (export)
            this.Arguments += string.Format(" -x ");

            // create Azure container 
            AzureHelper.CreateContainerIfNotExist(AzureHelper.StorageConnectionString, AzureHelper.BackupContainerName);
        }
    }
}
