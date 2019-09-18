using System;

namespace AzureWebjobHost
{
    public class ShutdownHandle : IDisposable
    {
        private readonly EventHandler _processExitEventHandler;
        private readonly ConsoleCancelEventHandler _cancelKeyPressEventHandler;

        public ShutdownHandle(EventHandler processExitEventHandler, ConsoleCancelEventHandler cancelKeyPressEventHandler)
        {
            _processExitEventHandler = processExitEventHandler;
            _cancelKeyPressEventHandler = cancelKeyPressEventHandler;

            AppDomain.CurrentDomain.ProcessExit += _processExitEventHandler;
            Console.CancelKeyPress += _cancelKeyPressEventHandler;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.ProcessExit -= _processExitEventHandler;
            Console.CancelKeyPress -= _cancelKeyPressEventHandler;
        }
    }

    public interface IShutdownHandleFactory
    {
        IDisposable Create(EventHandler processExitEventHandler, ConsoleCancelEventHandler cancelKeyPressEventHandler);
    }

    public class ShutdownHandleFactory : IShutdownHandleFactory
    {
        public IDisposable Create(EventHandler processExitEventHandler, ConsoleCancelEventHandler cancelKeyPressEventHandler)
        {
            return new ShutdownHandle(processExitEventHandler, cancelKeyPressEventHandler);
        }
    }
}
