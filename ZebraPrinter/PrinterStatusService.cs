using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


internal class PrinterStatusService : IHostedService, IDisposable
{

    private Timer _timer;
    private IPrinterInterface _printerInterface;

    public PrinterStatusService(IServiceProvider services)
    {
        Services = services;
        _printerInterface = Services.CreateScope().ServiceProvider.GetRequiredService<IPrinterInterface>();
        //using (var scope = Services.CreateScope())
        //{
        //    var scopedProcessingService =
        //        scope.ServiceProvider
        //            .GetRequiredService<IPrinterInterface>();

        //    scopedProcessingService.ReadAvailableData();
        //}
    }

    public IServiceProvider Services { get; }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Timed Background Service is starting.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        Console.WriteLine("Timed Background Service is working.");
        //_printerInterface.ReadAvailableData();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Timed Background Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

//public class QueuedHostedService : BackgroundService
//{
//    private readonly ILogger _logger;

//    public QueuedHostedService(IBackgroundTaskQueue taskQueue,
//        ILoggerFactory loggerFactory)
//    {
//        TaskQueue = taskQueue;
//        _logger = loggerFactory.CreateLogger<QueuedHostedService>();
//    }

//    public IBackgroundTaskQueue TaskQueue { get; }

//    protected async override Task ExecuteAsync(
//        CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Queued Hosted Service is starting.");

//        while (!cancellationToken.IsCancellationRequested)
//        {
//            var workItem = await TaskQueue.DequeueAsync(cancellationToken);

//            try
//            {
//                await workItem(cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                   $"Error occurred executing {nameof(workItem)}.");
//            }
//        }

//        _logger.LogInformation("Queued Hosted Service is stopping.");
//    }
//}