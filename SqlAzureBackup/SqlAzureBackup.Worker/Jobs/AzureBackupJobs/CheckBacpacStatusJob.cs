namespace SqlAzureBackup.Worker.Jobs.AzureBackupJobs
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Xml.Linq;

    public class CheckBacpacStatusJob : IJob<SqlAzureBackupJobContext>
    {
        public SqlAzureBackupJobContext Context { get; set; }

        public void Run()
        {
            Trace.WriteLine("Starting job: checking bacpac status..", "Info");

            string datacenterEndpoint = this.Context.DatacenterEndpoint;
            string serverName = AzureHelper.ServerName;
            string username = AzureHelper.SqlAzureUsername;
            string password = AzureHelper.SqlAzurePassword;
            string requestId = this.Context.BacpacRequestId;

            if (string.IsNullOrEmpty(datacenterEndpoint))
            {
                Trace.WriteLine("The datacenter endpoint value is empty", "Error");
                return;
            }

            if (string.IsNullOrEmpty(requestId))
            {
                Trace.WriteLine("The requestId value is empty", "Error");
                return;
            }

            try
            {
                // check the status several times until we get a "Completed" or "Failed" status
                bool exportOperationCompleted = false;            
                do
                {
                    string requestAction = string.Format("Status?servername={0}&username={1}&password={2}&reqid={3}", serverName, username, password, requestId);
                    var request = WebRequest.Create(string.Format("{0}/{1}", datacenterEndpoint, requestAction));
                    request.Method = WebRequestMethods.Http.Get;
                    request.ContentType = "application/xml";

                    using (var response = request.GetResponse())
                    {
                        Stream responseStream = response.GetResponseStream();
                        
                        using (var reader = new StreamReader(responseStream))
                        {
                            // find the status of the export
                            var xmlDoc = XDocument.Parse(reader.ReadToEnd());

                            string status = (from element in xmlDoc.Descendants()
                                             where element.Name.LocalName.ToLower() == "status"
                                             select element.Value.ToLower()).FirstOrDefault();

                            if (status == "completed")
                            {
                                string logBlobName = string.Format("log-{0}.txt", this.Context.BacpacBlobName);
                                AzureHelper.SaveTextToBlob(AzureHelper.StorageConnectionString, AzureHelper.BackupContainerName, logBlobName, xmlDoc.ToString());

                                Trace.WriteLine("Export job completed", "Info");
                                this.Context.BackupStatus = OperationStatus.Success;
                            }

                            if (status == "failed")
                            {
                                string errorMessage = (from element in xmlDoc.Descendants()
                                                       where element.Name.LocalName.ToLower() == "ErrorMessage"
                                                       select element.Value).FirstOrDefault();
                                
                                Trace.WriteLine("Job failed.\n Info: {0}", errorMessage);
                                this.Context.BackupStatus = OperationStatus.Failed;
                            }

                            exportOperationCompleted = status == "completed" || status == "failed";

                            // if we have to request the status again, delay it for 5 second
                            if (!exportOperationCompleted)
                            {
                                Trace.WriteLine("Export job still in progress");
                                Thread.Sleep(5000);
                            }
                        }
                    }
                } while (!exportOperationCompleted);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message, "Error");
                return;
            }
        }
    }
}