using System.Diagnostics;
using System;

namespace SqlAzureBackup.Worker.Jobs.Interfaces
{
    public class CommandLineJob : IJob
    {
        public string ProcessName { get; set; }
        public string TraceInfoMessage { get; set; }
        public string Arguments { get; set; }

        public CommandLineJob()
        {
            this.TraceInfoMessage = "Starting Job";
        }

        public void Run()
        {
            if (string.IsNullOrEmpty(this.ProcessName))
            {
                return;
            }

            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.EnableRaisingEvents = true;

            process.StartInfo.FileName = this.ProcessName;
            process.StartInfo.Arguments = this.Arguments;

            // set up handlers to receive errors & output asynchronously
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);
            process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            process.Exited += new EventHandler(Exited);

            // start the process
            Trace.WriteLine(this.TraceInfoMessage, "Info");
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            process.Close();
        }

        protected virtual void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Trace.WriteLine(e.Data, "Error");
            }
        }

        protected virtual void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // do something if needed
        }

        protected virtual void Exited(object sender, EventArgs e)
        {
            Trace.WriteLine("Job completed", "Info");
        }
    }
}
