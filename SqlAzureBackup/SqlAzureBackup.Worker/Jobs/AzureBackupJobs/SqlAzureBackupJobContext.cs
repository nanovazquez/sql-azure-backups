namespace SqlAzureBackup.Worker.Jobs.AzureBackupJobs
{
    public enum OperationStatus { Failed, Success };

    public class SqlAzureBackupJobContext : IJobContext
    {
        public string DatacenterEndpoint { get; set; }
        public string BacpacRequestId { get; set; }
        public string BacpacBlobName { get; set; }
        public OperationStatus BackupStatus { get; set; }
    }
}
