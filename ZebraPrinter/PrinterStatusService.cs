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
    }

    public IServiceProvider Services { get; }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Timed Background Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(10));
        return Task.CompletedTask;

    }

    private void DoWork(object state)
    {
        _printerInterface.ReadAvailableData();
        _printerInterface.WriteAvailableData();

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
