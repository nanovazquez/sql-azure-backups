namespace SqlAzureBackup.Worker
{
    using System;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.StorageClient;

    public static class AzureHelper
    {
        public static string StorageConnectionString = RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString");
        public static string ServerName = RoleEnvironment.GetConfigurationSettingValue("serverName");
        public static string DatacenterName = RoleEnvironment.GetConfigurationSettingValue("datacenterName");
        public static string DatabaseName = RoleEnvironment.GetConfigurationSettingValue("databaseName");
        public static string SqlAzureUsername = RoleEnvironment.GetConfigurationSettingValue("sqlAzureUsername");
        public static string SqlAzurePassword = RoleEnvironment.GetConfigurationSettingValue("sqlAzurePassword");
        public static string BlobUrl = RoleEnvironment.GetConfigurationSettingValue("blobUrl");
        public static string BlobAccessKey = RoleEnvironment.GetConfigurationSettingValue("blobAccessKey");
        public static string BackupContainerName = RoleEnvironment.GetConfigurationSettingValue("backupContainer");
        public static int BackupExecutionHour = Int32.Parse(RoleEnvironment.GetConfigurationSettingValue("backupExecutionHour"));
        public static int BackupFrequencyInHours = Int32.Parse(RoleEnvironment.GetConfigurationSettingValue("backupFrequencyInHours"));
        public static string FtpServerUrl = RoleEnvironment.GetConfigurationSettingValue("ftpServerUrl");
        public static string FtpServerUsername = RoleEnvironment.GetConfigurationSettingValue("ftpServerUsername");
        public static string FtpServerPassword = RoleEnvironment.GetConfigurationSettingValue("ftpServerPassword");

        public static CloudBlobContainer GetContainer(string connectionString, string containerName)
        {
            // Retrieve storage account from connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client 
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container 
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Create the container if it doesn't already exist
            bool isNewContainer = container.CreateIfNotExist();

            // Set default permissions
            if (isNewContainer)
            {
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });
            }

            return container;
        }

        public static string GetTextBlob(string connectionString, string containerName, string blobName)
        {
            string toReturn = string.Empty;

            // Create the container if it doesn't already exist
            var container = AzureHelper.GetContainer(connectionString, containerName);

            // Retrieve reference to a blob
            CloudBlob blob = container.GetBlobReference(blobName);

            try
            {
                toReturn = blob.DownloadText();
            }
            catch (Exception e)
            {
                if ((e as StorageClientException ).ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    // the blob does not exist
                }
            }

            return toReturn;
        }


        public static byte[] GetByteBlob(string connectionString, string containerName, string blobName)
        {
            byte[] toReturn = null;

            // Create the container if it doesn't already exist
            var container = AzureHelper.GetContainer(connectionString, containerName);

            // Retrieve reference to a blob
            CloudBlob blob = container.GetBlobReference(blobName);

            try
            {
                toReturn = blob.DownloadByteArray();
            }
            catch (Exception e)
            {
                if ((e as StorageClientException).ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    // the blob does not exist
                }
            }

            return toReturn;
        }

        public static void SaveTextToBlob(string connectionString, string containerName, string blobName, string textContent)
        {
            var container = AzureHelper.GetContainer(connectionString, containerName);

            if (container != null)
            {
                // Retrieve reference to a blobf
                CloudBlob blob = container.GetBlobReference(blobName);

                blob.UploadText(textContent);
            }
        }
    }
}
