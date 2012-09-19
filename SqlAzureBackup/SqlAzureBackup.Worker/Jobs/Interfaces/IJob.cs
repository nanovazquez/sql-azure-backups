namespace SqlAzureBackup.Worker.Jobs.Interfaces
{
    public interface IJob
    {
        IJobContext Context { get; set; }
        void Run();
    }
}
