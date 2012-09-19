using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Dac;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;
using System.IO;

namespace DacImportExportCli
{
    public partial class Program
    {
        public void ExtractAction()
        {
            ServerConnection connection = this.GetServerConnection(this.database);

            DacStore dacStore = null;

            Stopwatch sw = new Stopwatch();

            Console.WriteLine("Extract started: {0}", DateTime.Now);

            try
            {
                sw.Start();

                // Get a DacStore for the current connection
                dacStore = new DacStore(connection);

                this.EventSubscribe(dacStore);

                DacExtractionUnit deu = new DacExtractionUnit(connection, this.database);

                deu.Version = new Version(1, 0, 0, 0);
                deu.Description = string.Empty;
                deu.TypeName = this.database;

                DacExtractValidationResult result = deu.Extract(this.fileName);

                if (result.ErrorObjects.Count > 0)
                {
                    foreach (var error in result.ErrorObjects)
                    {
                        Console.WriteLine("[ERROR] {0}: {1}", error.Name, error.Description);
                    }
                }

                sw.Stop();

                FileInfo fi = new FileInfo(this.fileName);

                Console.WriteLine("Extract Complete.  Total time: {0}", sw.Elapsed.ToString());
                Console.WriteLine("Output file: {0} Size: {1} bytes", this.fileName, fi.Length);
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
                if (dacStore != null)
                {
                    this.EventUnsubscribe(dacStore);
                }
            }
        }
    }
}
