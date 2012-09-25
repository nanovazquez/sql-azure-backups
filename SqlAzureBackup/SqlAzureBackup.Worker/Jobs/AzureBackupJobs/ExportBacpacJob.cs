namespace SqlAzureBackup.Worker.Jobs.AzureBackupJobs
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Xml;
    using System.Xml.Linq;

    public class ExportBacpacJob : IJob<SqlAzureBackupJobContext>
    {
        public SqlAzureBackupJobContext Context { get; set; }

        private string bacpacBlobName = string.Empty;

        public void Run()
        {
            Trace.WriteLine("Starting job: exporting bacpac..", "Info");

            if (string.IsNullOrEmpty(this.Context.DatacenterEndpoint))
            {
                Trace.WriteLine("The datacenter endpoint value is empty", "Error");
                return;
            }

            // set up blob name
            bacpacBlobName = string.Format("backup-{0}.bacpac", DateTime.Now.ToString("MM-dd-yyyy-HH:mm"));

            // create container if not exist
            AzureHelper.GetContainer(AzureHelper.StorageConnectionString, AzureHelper.BackupContainerName);

            // send the export request
            try
            {

                // initialize POST body
                var bodyDocument = this.GetPostBody();

                var request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/Export", this.Context.DatacenterEndpoint));
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/xml; charset=utf-8";
                request.KeepAlive = true;

                using (var stream = request.GetRequestStream())
                {
                    using (var writer = new StreamWriter(stream))  
                    {
                        writer.Write(bodyDocument.InnerXml);  
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream responseStream = response.GetResponseStream();
                        using (var reader = new StreamReader(responseStream))
                        {
                            string requestId = XDocument.Parse(reader.ReadToEnd()).Root.Value;
                            this.Context.BacpacRequestId = requestId;
                            this.Context.BacpacBlobName = this.bacpacBlobName;
                            Trace.WriteLine(string.Format("Export request sent. RequestId: {0}", requestId), "Info");
                        }
                    }
                    else
                    {
                        Trace.WriteLine(string.Format("The Sql Azure Dac Service returned {0}", response.StatusDescription), "Error");
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message, "Error");
                return;
            }
        }

        public XmlDocument GetPostBody()
        {
            var document = new XmlDocument();
            var elementsNamespace = "http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes";
            var iNamespace = "http://www.w3.org/2001/XMLSchema-instance";

            // ExportInput element
            var rootElement = document.CreateElement("ExportInput", elementsNamespace);
            rootElement.SetAttribute("xmlns", elementsNamespace);
            rootElement.SetAttribute("xmlns:i", iNamespace);
            document.AppendChild(rootElement);

            // BlobCredentials section
            var blobCredentialsElement = document.CreateElement("BlobCredentials", elementsNamespace);
            var iTypeAttribute = document.CreateAttribute("i:type", iNamespace);
            iTypeAttribute.Value = "BlobStorageAccessKeyCredentials";
            blobCredentialsElement.Attributes.Append(iTypeAttribute);
            rootElement.AppendChild(blobCredentialsElement);

            string bacpacBlobUri = string.Format("{0}/{1}/{2}", AzureHelper.BlobUrl, AzureHelper.BackupContainerName, this.bacpacBlobName);
            var uriElement = document.CreateElement("Uri", elementsNamespace);
            uriElement.InnerText = bacpacBlobUri;
            blobCredentialsElement.AppendChild(uriElement);

            var storageAccesskeyElement = document.CreateElement("StorageAccessKey", elementsNamespace);
            storageAccesskeyElement.InnerText = AzureHelper.BlobAccessKey;
            blobCredentialsElement.AppendChild(storageAccesskeyElement);

            // ConnectionInfo section
            var connectionInfoElement = document.CreateElement("ConnectionInfo", elementsNamespace);
            rootElement.AppendChild(connectionInfoElement);

            var databaseNameElement = document.CreateElement("DatabaseName", elementsNamespace);
            databaseNameElement.InnerText = AzureHelper.DatabaseName;
            connectionInfoElement.AppendChild(databaseNameElement);

            var passwordElement = document.CreateElement("Password", elementsNamespace);
            passwordElement.InnerText = AzureHelper.SqlAzurePassword;
            connectionInfoElement.AppendChild(passwordElement);

            var serverNameElement = document.CreateElement("ServerName", elementsNamespace);
            serverNameElement.InnerText = AzureHelper.ServerName;
            connectionInfoElement.AppendChild(serverNameElement);

            var usernameElement = document.CreateElement("UserName", elementsNamespace);
            usernameElement.InnerText = AzureHelper.SqlAzureUsername;
            connectionInfoElement.AppendChild(usernameElement);
            
            return document;
        }
    }
}
