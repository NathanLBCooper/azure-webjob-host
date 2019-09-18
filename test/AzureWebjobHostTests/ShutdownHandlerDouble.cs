using AzureWebjobHost;
using System;
using System.Threading.Tasks;

namespace AzureWebjobHostTests
{
    public class ShutdownHandleDouble : IDisposable
    {
        public EventHandler ProcessExitEventHandler { get; set; }
        public ConsoleCancelEventHandler CancelKeyPressEventHandler { get; set; }

        public ShutdownHandleDouble()
        {
        }

        public void Dispose()
        {
        }

        public void ProcessExitIn(int delayMs)
        {
            Task.Delay(delayMs).ContinueWith(t => ProcessExitEventHandler.Invoke(default, new EventArgs()));
        }

        public void CancelKeyPressIn(int delayMs)
        {
            Task.Delay(delayMs).ContinueWith(t => CancelKeyPressEventHandler.Invoke(default, CreateConsoleCancelEventArgs()));
        }

        private static ConsoleCancelEventArgs CreateConsoleCancelEventArgs()
        {
            var eventArg = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ConsoleCancelEventArgs)) as ConsoleCancelEventArgs;
            return eventArg;
        }
    }

    public class ShutdownHandleDoubleFactory : IShutdownHandleFactory
    {
        private readonly ShutdownHandleDouble _shutdownHandle;
        private bool _created = false;

        public ShutdownHandleDoubleFactory(ShutdownHandleDouble shutdownHandle)
        {
            _shutdownHandle = shutdownHandle;
        }

        public IDisposable Create(EventHandler processExitEventHandler, ConsoleCancelEventHandler cancelKeyPressEventHandler)
        {
            if (_created) throw new NotSupportedException("This test double wasn't made to be called twice");

            _shutdownHandle.ProcessExitEventHandler = processExitEventHandler;
            _shutdownHandle.CancelKeyPressEventHandler = cancelKeyPressEventHandler;

            return _shutdownHandle;
        }
    }
}
