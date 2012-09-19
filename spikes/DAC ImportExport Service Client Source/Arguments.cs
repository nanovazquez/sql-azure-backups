using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DacImportExportCli
{
    public partial class Program
    {
        /// <summary>
        /// This method processes the command line arguments and determines what action should occur.
        /// </summary>
        /// <param name="args">The list of command line arguments.</param>
        /// <returns>An action to perform or HELP if there was an error.</returns>
        internal Action ProcessCommandLine(string[] args)
        {
            Action result = Action.Invalid;

            if (args.Length == 0)
                result = Action.Help;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '/' || args[i][0] == '-')
                {
                    string command = args[i].Substring(1);
                    switch (command.ToUpper())
                    {
                        case "SERVER":
                        case "S":
                            serverName = args[i + 1];
                            i++;
                            break;

                        case "E":
                            useWindowsAuth = true;
                            break;

                        case "USER":
                        case "U":
                            userName = args[i + 1];
                            i++;
                            break;

                        case "PASSWORD":
                        case "P":
                            password = args[i + 1];
                            i++;
                            break;

                        case "EXPORT":
                        case "X":
                            result = Action.Export;
                            break;

                        case "DATABASE":
                        case "D":
                            database = args[i + 1];
                            i++;
                            break;

                        case "IMPORT":
                        case "I":
                            result = Action.Import;
                            break;

                        case "FILENAME":
                        case "F":
                            fileName = args[i + 1];
                            i++;
                            break;

                        case "DROP":
                            result = Action.Drop;
                            break;

                        case "EDITION":
                            string edstring = args[i + 1];
                            i++;
                            switch (edstring.ToUpper())
                            {
                                case "BUSINESS":
                                    this.azureEdition = Microsoft.SqlServer.Management.Dac.AzureEdition.Business;
                                    break;
                                case "WEB":
                                    this.azureEdition = Microsoft.SqlServer.Management.Dac.AzureEdition.Web;
                                    break;
                                default:
                                    throw new ArgumentException("Invalid argument for SQL Azure Edition");
                            }
                            break;

                        case "SIZE":
                            int size = int.Parse(args[i + 1]);
                            if (size > 0)
                            {
                                this.azureSize = size;
                            }
                            i++;
                            break;
                        
                        case "N":
                            this.useSSL = true;
                            break;

                        case "T":
                            this.trustServerCertificate = true;
                            break;
                        
                        case "EXTRACT":
                            result = Action.Extract;
                            break;

                        case "DEPLOY":
                            result = Action.Deploy;
                            break;

                        case "HELP":
                        case "H":
                        case "?":
                        default:
                            result = Action.Help;
                            break;
                    }
                }
                else
                {
                    result = Action.Help;
                    break;
                }
            }

            ArgumentsValidater(result);

            return result;
        }

        internal void ArgumentsValidater(Action action)
        {
            if (action == Action.Invalid)
            {
                throw new ArgumentException("No operation specified export, import, drop, etc.");
            }

            if (this.useWindowsAuth == true && (!string.IsNullOrEmpty(this.userName) || !string.IsNullOrEmpty(this.password)))
            {
                throw new ArgumentException("Windows Authentication is not valid with a specified username or password. Only specify only authentication type (-e or -u).");
            }

            if (action == Action.Import || action == Action.Export || action == Action.Drop || action == Action.Extract || action == Action.Deploy)
            {
                if (serverName == null)
                {
                    throw new ArgumentException("Server name must be specified.");
                }

                if (database == null)
                {
                    throw new ArgumentException("Database name must be specified.");
                }
            }

            if (action == Action.Import || action == Action.Export || action == Action.Extract || action == Action.Deploy)
            {
                if (fileName == null)
                {
                    throw new ArgumentException("Bacpac filename must be specified.");
                }
            }

            // Can't set the azure flags unless we are doing an import
            if (action != Action.Import)
            {
                if (this.azureEdition != Microsoft.SqlServer.Management.Dac.AzureEdition.Default || this.azureSize != -1)
                {
                    throw new ArgumentException("Azure Edition and Size can only be set during Import operations.");
                }
            }

            if (action == Action.Import && this.azureEdition != Microsoft.SqlServer.Management.Dac.AzureEdition.Default)
            {
                if (this.useWindowsAuth)
                {
                    throw new ArgumentException("Cannot use Windows Auth when connecting to Azure.");
                }
            }

            if (!string.IsNullOrEmpty(this.fileName))
            {
                if ((action == Action.Import || action == Action.Deploy) && !System.IO.File.Exists(this.fileName))
                {
                    throw new ArgumentException(string.Format("Filename {0} does not appear to exist.", this.fileName));
                }
            }
        }
    }
}
