namespace SqlAzureBackup.Worker.Jobs.AzureBackupJobs
{
    using System.Diagnostics;

    public class ResolveDatacenterJob : IJob<SqlAzureBackupJobContext>
    {
        public SqlAzureBackupJobContext Context { get; set; }

        public void Run()
        {
            Trace.WriteLine("Starting job: resolving Datacenter..", "Info");

            string datacenterName = AzureHelper.DatacenterName;
            string datacenterEndpoint = string.Empty;

            switch (datacenterName.ToLower())
            {
                case "north central us":
                    datacenterEndpoint = "https://ch1prod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "south central us":
                    datacenterEndpoint = "https://sn1prod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "north europe":
                    datacenterEndpoint = "https://db3prod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "west europe":
                    datacenterEndpoint = "https://am1prod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "east asia":
                    datacenterEndpoint = "https://hkgprod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "southeast asia":
                    datacenterEndpoint = "https://sg1prod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "east us":
                    datacenterEndpoint = "https://bl2prod-dacsvc.azure.com/DACWebService.svc";
                    break;
                case "west us":
                    datacenterEndpoint = "https://by1prod-dacsvc.azure.com/DACWebService.svc";
                    break;
            }

            if (!string.IsNullOrEmpty(datacenterEndpoint))
            {
                this.Context.DatacenterEndpoint = datacenterEndpoint;
                Trace.WriteLine(string.Format("Endpoint: {0}", datacenterEndpoint), "Info");
                Trace.WriteLine("Datacenter endpoint resolved", "Info");
            }
            else
            {
                Trace.WriteLine(string.Format("The datacenter name {0} is invalid (or is a new datacenter)", datacenterName), "Error");
            }
        }
    }
}
