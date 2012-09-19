using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Dac;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using System.IO;

namespace DacImportExportCli
{
    public partial class Program
    {
        public void ExportAction()
        {
            ServerConnection connection = this.GetServerConnection(this.database);

            DacStore dacStore = null;

            Stopwatch sw = new Stopwatch();

            Console.WriteLine("Export started: {0}", DateTime.Now);

            try
            {
                sw.Start();

                // Get a DacStore for the current connection
                dacStore = new DacStore(connection);

                this.EventSubscribe(dacStore);

                dacStore.Export(this.database, this.fileName);

                sw.Stop();

                FileInfo fi = new FileInfo(this.fileName);

                Console.WriteLine("Export Complete.  Total time: {0}", sw.Elapsed.ToString());
                Console.WriteLine("Output file: {0} Size: {1} bytes", this.fileName, fi.Length);
            }
            catch (BacpacException bacpacex)
            {
                Console.WriteLine("BACPAC Exception: {0}", bacpacex);
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
                this.EventUnsubscribe(dacStore);
            }
        }
    }
}
