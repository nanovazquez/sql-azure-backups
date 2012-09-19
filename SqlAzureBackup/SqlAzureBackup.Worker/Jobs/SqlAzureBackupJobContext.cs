using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlAzureBackup.Worker.Jobs.Interfaces;

namespace SqlAzureBackup.Worker.Jobs
{
    public class SqlAzureBackupJobContext : IJobContext
    {
        public string BacpacBlobUri { get; set; }
        public string BacpacBlobName { get; set; }
    }
}
