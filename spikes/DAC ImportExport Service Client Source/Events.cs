using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Dac;

namespace DacImportExportCli
{
    public partial class Program
    {
        internal void EventUnsubscribe(DacStore dacStore)
        {
            if (dacStore != null)
            {
                // The finished action is fired after a dac action has completed
                dacStore.DacActionFinished -= new EventHandler<DacActionEventArgs>(dacStore_DacActionFinished);

                // The initialized event is fired when a dac action object is allocated (verbose)
                //dacStore.DacActionInitialized -= new EventHandler<DacActionEventArgs>(dacStore_DacActionInitialized);

                // The started action is fired before any work has started on the event
                dacStore.DacActionStarted -= new EventHandler<DacActionEventArgs>(dacStore_DacActionStarted);
            }
        }

        internal void EventSubscribe(DacStore dacStore)
        {
            if (dacStore != null)
            {
                // The finished action is fired after a dac action has completed
                dacStore.DacActionFinished += new EventHandler<DacActionEventArgs>(dacStore_DacActionFinished);

                // The initialized event is fired when a dac action object is allocated (verbose)
                //dacStore.DacActionInitialized += new EventHandler<DacActionEventArgs>(dacStore_DacActionInitialized);

                // The started action is fired before any work has started on the event
                dacStore.DacActionStarted += new EventHandler<DacActionEventArgs>(dacStore_DacActionStarted);
            }
        }

        static void dacStore_DacActionFinished(object sender, DacActionEventArgs e)
        {
            if (e.ActionState == ActionState.Warning)
            {
                Console.WriteLine("[{0}] {1}: {2} {3}", DateTime.Now.ToString("T"), e.ActionName, e.ActionState, e.Description);
            }
            else
            {
                Console.WriteLine("[{0}] {1}: {2} {3} {4}", DateTime.Now.ToString("T"), e.ActionName, e.ActionState, e.Description, e.Error);
            }
        }

        static void dacStore_DacActionInitialized(object sender, DacActionEventArgs e)
        {
            Console.WriteLine("[{0}] {1}: {2} {3} {4}", DateTime.Now.ToString("T"), e.ActionName, e.ActionState, e.Description, e.Error);
        }

        static void dacStore_DacActionStarted(object sender, DacActionEventArgs e)
        {
            Console.WriteLine("[{0}] {1}: {2} {3} {4}", DateTime.Now.ToString("T"), e.ActionName, e.ActionState, e.Description, e.Error);
        }

        /// <summary>
        /// Event handler that gets called when there is an unhandled exception within the
        /// process.
        /// </summary>
        /// <param name="sender">The class that generated the event.</param>
        /// <param name="e">The unhandled exception event arguments.</param>
        void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled Exception: {0}", e.ExceptionObject.ToString());
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            System.Environment.Exit(-1);
        }
    }
}
