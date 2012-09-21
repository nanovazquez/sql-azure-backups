namespace SqlAzureBackup.Worker.Jobs.AzureBackupJobs
{
    using System.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using System;
    using System.Text;
    using System.Net;
    using System.Dynamic;
 
    public class SendFileViaFTP : IJob<SqlAzureBackupJobContext>
    {
        public SqlAzureBackupJobContext Context { get; set; }

        public void Run()
        {
            Trace.WriteLine("Sending file via FTP..", "Info");

            if (this.Context.BackupStatus != OperationStatus.Success)
            {
                Trace.WriteLine("There backup status is not successfull", "Error");
                return;
            }

            // get blob name from context
            string blobName = this.Context.BacpacBlobName;

            if (string.IsNullOrEmpty(blobName))
            {
                Trace.WriteLine("There's no blobName stored", "Error");
                return;
            }

            var blob = AzureHelper.GetByteBlob(AzureHelper.StorageConnectionString, AzureHelper.BackupContainerName, blobName);

            if (blob == null)
            {
                Trace.WriteLine("The blob does not exist!", "Error");
                return;
            }

            string serverUrl = string.Format("ftp://{0}/{1}", AzureHelper.FtpServerUrl, blobName);
            string username = AzureHelper.FtpServerUsername;
            string password = AzureHelper.FtpServerPassword;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUrl);
            request.Proxy = null;
            request.UseBinary = true;
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(username, password);

            try
            {
                // Read the file from the server & write to destination                
                using (var ftpStream = request.GetRequestStream())
                {

                    int bytesSent = 0;
                    while (bytesSent < blob.Length)
                    {
                        int bytesToWrite = (blob.Length - bytesSent >= 102400) ? 102400 : blob.Length - bytesSent;
                        ftpStream.Write(blob, bytesSent, bytesToWrite);
                        bytesSent += bytesToWrite;
                    }

                    Trace.WriteLine("Job completed", "Info");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message, "Error");
            }
        }
    }
}