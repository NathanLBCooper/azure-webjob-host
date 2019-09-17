# azure-webjob-host
Provides shutdown detection for your azure webjob without taking the whole Azure WebJobs SDK

Run your continuous service in a webjob like this:

    IHostedService myService = new MyService();
    
    using (var host = new ServiceHost(myService))
    {
        await host.RunAsync(default);
    }

    // Found in Microsoft.Extensions.Hosting
    // public interface IHostedService
    // {
    //    Task StartAsync(CancellationToken cancellationToken);
    //    Task StopAsync(CancellationToken cancellationToken);
    // }
    
 Or run a single method that takes and supports cancellationTokens
 
    async Task DoSomething(CancellationToken cancellationToken)
            {
              if (cancellationToken.IsCancellationRequested) return;

              for (int i = 0; i < 100; i++)
              {
                  if (cancellationToken.IsCancellationRequested) return;
                  await Task.Delay(100);
                  Console.WriteLine("hello world");
              }
            };
    
    using (var host = new JobHost())
    {
        await host.RunAsync(DoSomething);
    }
    
If the azure web job gets a shutdown indicator while your code is executing, the *ServiceHost* will call *StopAsync* on your service, or the *JobService* will cancel the token that was provided to your method.

