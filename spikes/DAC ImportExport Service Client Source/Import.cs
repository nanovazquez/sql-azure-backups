using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dac;

namespace DacImportExportCli
{
    public partial class Program
    {
        public void ImportAction()
        {
            DacStore dacStore = null;
            Stopwatch sw = new Stopwatch();

            try
            {
                Console.WriteLine("Import started: {0}", DateTime.Now);

                ServerConnection connection = this.GetServerConnection(null);

                sw.Start();

                dacStore = new DacStore(connection);

                this.EventSubscribe(dacStore);

                // Build the deployment properties.  
                DatabaseDeploymentProperties ddp = new DatabaseDeploymentProperties(connection, this.database);

                // Set the azure editions (defaults will accept standard Azure account settings)
                ddp.AzureEdition = this.azureEdition;

                if (this.azureSize > 0)
                {
                    ddp.AzureMaxSize = this.azureSize;
                }

                // DENALI CTP3 version and earlier use these arguments
                dacStore.Import(ddp, this.fileName);

                // DAC Versions after Aug 2011 will have these arguments
                // dacStore.Import(this.fileName, ddp);
            }
            catch (DacException dacex)
            {
                Console.WriteLine("DAC Exception: {0}", dacex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled Exception: {0}", ex);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine("Import Complete.  Total time: {0}", sw.Elapsed.ToString());

                this.EventUnsubscribe(dacStore);
            }
        }
    }
}
