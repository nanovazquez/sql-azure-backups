using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using SqlAzureBackup.Worker.Jobs.Interfaces;
using System;
using System.Text;
using System.Net;

namespace SqlAzureBackup.Worker.Jobs
{
    public class SendFileViaFTP : IJob
    {
        public void Run()
        {
            Trace.WriteLine("Sending file via FTP..", "Info");

            string blobName = string.Format("backup-{0}.bacpac", DateTime.Now.ToString("MM-dd-yyyy"));
            var blob = AzureHelper.GetByteBlob(AzureHelper.StorageConnectionString, AzureHelper.BackupContainerName, blobName);

            if (blob == null)
            {
                Trace.WriteLine("The blob does not exist!", "Info");
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

                    Trace.WriteLine("Completed", "Info");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("Error!\n {0}", e.Message), "Error");
            }
        }
    }
}