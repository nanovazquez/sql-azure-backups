using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dac;
using System.Collections.Specialized;

namespace DacImportExportCli
{
    public partial class Program
    {
        public void DropDACAction()
        {
            DacStore dacStore = null;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Console.WriteLine("Operation started: {0}", DateTime.Now);
            ServerConnection connection = this.GetServerConnection(null);

            try
            {
                dacStore = new DacStore(connection);
                dacStore.Uninstall(this.database, DacUninstallMode.DropDatabase);

                Console.WriteLine("Dac.Uninstall called to remove database and DAC references.");
            }
            catch
            {
                // We ignore all errors at this point - just trying to get rid of the database and any DAC registrations
                Console.WriteLine("[WARNING] DAC.Uninstall failed. DAC Registration may not be present.");

                try
                {
                    // If the database has already been dropped, but the DAC entry still exists we have to unmanage it
                    // Try to unmanage the Data Tier Application entry 
                    dacStore.Unmanage(this.database);

                    Console.WriteLine(Environment.NewLine);
                    Console.WriteLine("Dac.Unmanage called to remove DAC related history.");
                }
                catch
                {
                    Console.WriteLine("[WARNING] Database does not appear to be registered as a DAC.  DAC.Unmanage failed.");
                }

                try
                {
                    // This won't work on sql azure, so don't even try
                    if (connection.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase)
                    {

                        // Delete all the backuphistory if we can...
                        StringCollection sqlcommands = new StringCollection();
                        sqlcommands.Add(string.Format("EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{0}'", this.database));

                        connection.ExecuteNonQuery(sqlcommands, ExecutionTypes.ContinueOnError);

                        Console.WriteLine("Deleted database DAC history.");
                    }
                }
                catch
                {
                    Console.WriteLine("[WARNING] Failed on call sp_delete_database_backuphistory.  Server may not be DAC compatible.");
                }

                try
                {
                    // If there was a database, but it was not DAC managed, we should drop it
                    connection.ExecuteNonQuery(string.Format("DROP DATABASE [{0}]", this.database));

                    Console.WriteLine("Dropped database using SQL.");
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to drop database.  Verify database exists, and you have permissions to drop it.");
                }
            }
            finally
            {
                sw.Stop();
                Console.WriteLine("Operation Complete.  Total time: {0}", sw.Elapsed.ToString());
            }
        }
    }
}
