# azure-webjob-host
Provides shutdown detection for your azure webjob without taking the whole Azure WebJobs SDK.

----------

[![Build status](https://ci.appveyor.com/api/projects/status/gjlmtxmee7txgnfe?svg=true)](https://ci.appveyor.com/project/NathanLBCooper/azure-webjob-host)
![GitHub](https://img.shields.io/github/license/NathanLBCooper/azure-webjob-host.svg)

| Package | Version |
| --- | --- |
| **AzureWebjobHost** | [![nuget](https://img.shields.io/nuget/v/AzureWebjobHost.svg)](https://www.nuget.org/packages/AzureWebjobHost/) |

#### Background 

The way Azure notifies a WebJob process it's about to be stopped is by placing (creating) a file at a path defined by the environment variable `WEBJOBS_SHUTDOWN_FILE`. This means Azure is going to stop your process in 5 seconds time (although that time is configurable). If you to be able to react, so that you can gracefully shutdown, you'd better listen for it.

One way to do this is to use the [Azure WebJobs SDK](https://github.com/Azure/azure-webjobs-sdk/wiki).

But if you don't want to take that dependency, this is a lighter weight, less opinionated alternative.

#### How to use it

This library uses the *Microsoft.Extensions.Hosting* pattern to provide shutdown notifications to your continuous service. Just provide your class as a *IHostedService* to our *ServiceHost*


    IHostedService myService = new MyService();
    
    using (var host = new ServiceHostBuilder().HostService(myService))
    {
        await host.RunAsync(default);
    }

    // Found in Microsoft.Extensions.Hosting
    // public interface IHostedService
    // {
    //    Task StartAsync(CancellationToken cancellationToken);
    //    Task StopAsync(CancellationToken cancellationToken);
    // }
    
Or, if you just want to run a *Task* instead of a *IHostedService*, use the *JobHost* instead. Be sure to provide a *Task* that will act on the cancellation request.
 
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
    
    using (var host = new JobHostBuilder().Build())
    {
        await host.RunAsync(DoSomething);
        // var result = host.RunAsync(ReturnSomething) <- Also available with return values 
    }
    
#### What happens if the host detects a shutdown
    
If the Azure web job detects a shutdown while your code is executing the *ServiceHost* will call *StopAsync* on your service. If you're using the *JobService* instead, that will cancel the token that was provided to your method

In the case of a `WEBJOBS_SHUTDOWN_FILE` shutdown your code will have 5 seconds (configurable in Azure) to gracefully exit.

There are other types of exit however. The hosts are also listening for `AppDomain.CurrentDomain.ProcessExit` and `Console.CancelKeyPress`. Your code will be notified in the same way, and the host will attempt to block these types of shutdowns for up to 4 seconds while your code *inside the host.RunAsync* exits.

Note, that because this code it intended to allow your whole process to exit, but only directly tries to block shutdown while code inside the host is running, it's best to put the hosting code as close to the top of your application as possible. This is a "framework level concern", not something to mix in with your application code. I place it at the top of Main in my WebJob console apps.

