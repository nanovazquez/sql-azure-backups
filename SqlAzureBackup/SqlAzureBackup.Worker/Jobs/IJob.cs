namespace SqlAzureBackup.Worker.Jobs
{
    using System.Dynamic;

    public interface IJob<T> where T: IJobContext
    {
        T Context { get; set; }
        void Run();
    }
}
