using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dac;
using System.Reflection;
using System.Threading;
using System.Data.SqlClient;

namespace DacImportExportCli
{
    public partial class Program
    {
        /// <summary>
        /// This enumeration indicates the actions that can be initiated by the 
        /// command line.
        /// </summary>
        internal enum Action
        {
            /// <summary>
            /// The action is invalid.
            /// </summary>
            Invalid,

            /// <summary>
            /// The requested action is to show help for the tool.
            /// </summary>
            Help,

            /// <summary>
            /// Perform an deploy operation on a dacpac or bacpac
            /// </summary>
            Deploy,

            /// <summary>
            /// Perform an export operation on a bacpac
            /// </summary>
            Export,

            /// <summary>
            /// Perform an import operation on a bacpac
            /// </summary>
            Import,

            /// <summary>
            /// Drop the database, and it's DAC registration.  Attempts to drop each if the DAC methods fail.
            /// </summary>
            Drop,

            /// <summary>
            /// Extract a database to a dacpac (schema only).  This can be used to determine if a database will work against Sql Azure.
            /// </summary>
            Extract
        }

        private string serverName;
        private string userName;
        private string password; 
        private bool useWindowsAuth = false;
        private string database;
        private string fileName;
        private AzureEdition azureEdition = AzureEdition.Default;
        private int azureSize = -1;
        private bool useSSL = false;
        private bool trustServerCertificate = false;

        static void Main(string[] args)
        {
            Program p = new Program();
            p.RunProgram(args);
        }

        /// <summary>
        /// This initializes the process, parses the command line, and handles exceptions.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        internal void RunProgram(string[] args)
        {
            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(this.Program_UnhandledException);

            Program p = new Program();
            p.ShowLogo();

            try
            {
                switch (p.ProcessCommandLine(args))
                {
                    case Action.Export:
                        p.ExportAction();
                        break;
                    case Action.Import:
                        p.ImportAction();
                        break;
                    case Action.Drop:
                        p.DropDACAction();
                        break;
                    case Action.Extract:
                        p.ExtractAction();
                        break;
                    case Action.Deploy:
                        p.DeployAction();
                        break;
                    case Action.Help:
                    default:
                        p.Usage();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error has occurred:");
                Console.WriteLine(e.Message);
                Exception inner = e.InnerException;
                while (inner != null)
                {
                    Console.WriteLine(inner.Message);
                    inner = inner.InnerException;
                    Console.WriteLine(string.Empty);
                }
                Console.WriteLine(string.Empty);
            }
        }

        /// <summary>
        /// This method shows copyright and version information for the tool in the console window.
        /// </summary>
        public void ShowLogo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Console.WriteLine(Properties.Resources.Logo1, assembly.GetName().Version.ToString());
            Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.");
            Console.WriteLine("");
        }

        /// <summary>
        /// This method writes out help information on how to use the tool to the console window.
        /// </summary>
        public void Usage()
        {
            Console.WriteLine("Command Line Parameters:");
            Console.WriteLine("-H[elp] | -?             Show this help text.");
            Console.WriteLine("-X[export]               Perform an export action.");
            Console.WriteLine("-I[mport]                Perform an import action.");
            Console.WriteLine("-D[atabase] <database>   Database name to perform the action on.");
            Console.WriteLine("-F[ile] <filename>       Name of the backup file.");
            Console.WriteLine("-S[erver] <servername>   SQL Server Name and instance.");
            Console.WriteLine("-E                       Use Windows authentication");
            Console.WriteLine("                         (not valid for SQL Azure)");
            Console.WriteLine("-U[ser]                  User name for SQL authentication.");
            Console.WriteLine("-P[assword]              Password for SQL authentication.");
            Console.WriteLine("-DROP                    Drop a database and remove the DAC registration.(*2)");
            Console.WriteLine("-EDITION <business|web>  SQL Azure edition to use during database creation.(*4)");
            Console.WriteLine("-SIZE <1>                SQL Azure database size in GB.(*4)");
            Console.WriteLine("-N                       Encrypt Connection using SSL.");
            Console.WriteLine("-T                       Force TrustServerCertificate(*6)");
            Console.WriteLine("-EXTRACT                 Extract database schema only.");
            Console.WriteLine("-DEPLOY                  Deploy schema only to database.");
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("Usage:");
            Console.WriteLine("Export a database to a bacpac using Windows Authentication:");
            Console.WriteLine("   {0} -S myserver -E -B -D northwind -F northwind.bacpac -X", this.AppName);
            Console.WriteLine(Environment.NewLine);

            Console.WriteLine("Import a bacpac to a database using Windows Authentication:");
            Console.WriteLine("   {0} -S myserver -E -D nw_restored -F northwind.bacpac -I", this.AppName);
            Console.WriteLine(Environment.NewLine);

            Console.WriteLine("Import a bacpac to SQL Azure with options:");
            Console.WriteLine("   {0} -S myazure -U azureuser -P azurepwd -D nw_restored -F northwind.bacpac -I -EDITION web -SIZE 5 -N", this.AppName);
            Console.WriteLine(Environment.NewLine);

            Console.WriteLine("Drop both database and DAC registration:");
            Console.WriteLine("   {0} -S myserver -U azureuser -P azurepwd -D mydb -DROP", this.AppName);
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("*Notes and caveats:");
            Console.WriteLine("1. On import the database must not exist.  A new database is");
            Console.WriteLine("   always created. SQL Azure uses system edition defaults if not set.");
            Console.WriteLine("2. DROP is very aggressive. It will attempt to remove a database");
            Console.WriteLine("   that is not registered as a DAC.  It will also remove DAC");
            Console.WriteLine("   registration missing a database.  Use -D to specify the database.");
            Console.WriteLine("3. Databases can use this tool only if they contain ");
            Console.WriteLine("   SQL 2008 R2 DAC supported objects and types.");
            Console.WriteLine("4. Choose the SQL Azure options desired, this may impact billing.");
            Console.WriteLine("   (Only valid against SQL Azure)");
            Console.WriteLine("5. See SQL Server Books Online Topic: ");
            Console.WriteLine("   Understanding Data-tier Applications ");
            Console.WriteLine("   for more information about DAC.");
            Console.WriteLine("6. Used to resolve connection issues when receiving exception:");
            Console.WriteLine("   'certificate's CN name does not match the passed value'");
            Console.WriteLine("   See SQL Server Books Online Topic: TrustServerCertificate");
            Console.WriteLine(Environment.NewLine);
        }

        internal ServerConnection GetServerConnection(string databaseName)
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();

            csb.DataSource = this.serverName;

            if (this.trustServerCertificate)
            {
                csb.TrustServerCertificate = true;
            }
            
            if (!string.IsNullOrEmpty(databaseName))
            {
                csb.InitialCatalog = databaseName;
            }

            if (this.useSSL)
            {
                csb.Encrypt = true;
            }
            else
            {
                csb.Encrypt = false;
            }

            if (this.useWindowsAuth == false)
            {
                if (userName == null)
                {
                    throw new ArgumentException("User name must be specified unless Windows Authentication (-E) is specified.");
                }
                if (password == null)
                {
                    throw new InvalidArgumentException("User password must be specified unless Windows Authentication (-E) is specified.");
                }
                csb.IntegratedSecurity = false;
                csb.UserID = this.userName;
                csb.Password = this.password;
            }
            else
            {
                csb.IntegratedSecurity = true;
            }

            csb.ConnectTimeout = 60;
            csb.MinPoolSize = 5;
            csb.MaxPoolSize = 50;        // According to Azure team there is NO max limit on connections.  It all comes down to CPU and IO usage.  They said setting this to 100 would be fine.
            csb.Pooling = true;

            // If we think we are talking to Azure we should turn on encryption if the user didn't set it
            if ( (this.azureEdition != AzureEdition.Default || this.azureSize != -1) && this.useSSL == false )
            {
                Console.WriteLine("Turning on Encryption for SQL Azure");
                csb.Encrypt = true;
            }

            ServerConnection connection = new ServerConnection();
            connection.ConnectionString = csb.ToString();

            connection.StatementTimeout = 60 * 60 * 24;
            Console.WriteLine("Connecting to {0}...", this.serverName);
            connection.Connect();
            Console.WriteLine("Connection Open.");
            return connection;
        }

        private string m_appName;

        internal string AppName
        {
            get
            {
                if (m_appName == null)
                {
                    string assemblyName = string.Empty;

                    Assembly assembly = Assembly.GetEntryAssembly();

                    // If called from unmanaged code (like MSTEST) the GetEntryAssembly will return null
                    if (assembly == null)
                    {
                        assembly = Assembly.GetExecutingAssembly();
                    }

                    if (assembly != null)
                    {
                        assemblyName = assembly.GetName().Name;
                    }

                    m_appName = assemblyName;
                }

                return m_appName;
            }
        }
    }
}
