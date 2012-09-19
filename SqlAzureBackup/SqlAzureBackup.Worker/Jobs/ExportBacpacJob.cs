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
            string bacpacBlobName = string.Format("backup-{0}.bacpac", DateTime.Now.ToString("MM-dd-yyyy"));
            string bacpacBlobUri = string.Format("{0}/{1}/{2}", AzureHelper.BlobUrl, AzureHelper.BackupContainerName, bacpacBlobName);

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

            // store the blob uri and blob name in the context
            SqlAzureBackupJobContext context = this.Context as SqlAzureBackupJobContext;
            context.BacpacBlobUri = bacpacBlobUri;
            context.BacpacBlobName = bacpacBlobName;
        }
    }
}
