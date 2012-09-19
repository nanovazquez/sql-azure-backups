using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dac;
using System.IO;

namespace DacImportExportCli
{
    public partial class Program
    {
        public void DeployAction()
        {
            DacStore dacStore = null;
            Stopwatch sw = new Stopwatch();

            try
            {
                DacType dacType = DacType.Load(File.Open(this.fileName, FileMode.Open));
                Console.WriteLine("Deploy started: {0}", DateTime.Now);

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

                dacStore.Install(dacType, ddp, true);
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
                Console.WriteLine("Deploy Complete.  Total time: {0}", sw.Elapsed.ToString());

                this.EventUnsubscribe(dacStore);
            }
        }
    }
}
