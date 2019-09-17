using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace AzureWebjobHost
{
    /// <summary>
    /// Helper class for providing a cancellation token for when this WebJob's shutdown is signaled.
    /// https://github.com/Azure/azure-webjobs-sdk/blob/master/src/Microsoft.Azure.WebJobs.Host/WebjobsShutdownWatcher.cs
    /// </summary>
    public sealed class WebJobsShutdownWatcher : IDisposable
    {
        private readonly string shutdownFile;
        private readonly bool ownsCancellationTokenSource;

        private CancellationTokenSource cts;
        private FileSystemWatcher watcher;

        /// <summary>
        /// Begin watching for a shutdown notification from Antares.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public WebJobsShutdownWatcher()
            : this(new CancellationTokenSource(), ownsCancellationTokenSource: true)
        {
        }

        private WebJobsShutdownWatcher(CancellationTokenSource cancellationTokenSource, bool ownsCancellationTokenSource)
        {
            // http://blog.amitapple.com/post/2014/05/webjobs-graceful-shutdown/#.U3aIXRFOVaQ
            // Antares will set this file to signify shutdown
            shutdownFile = Environment.GetEnvironmentVariable("WEBJOBS_SHUTDOWN_FILE");
            if (shutdownFile == null)
            {
                // If env var is not set, then no shutdown support
                return;
            }

            // Setup a file system watcher on that file's directory to know when the file is created
            string directoryName = Path.GetDirectoryName(shutdownFile);
            try
            {
                // FileSystemWatcher throws an argument exception if the part of 
                // the directory name does not exist
                watcher = new FileSystemWatcher(directoryName);
            }
            catch (ArgumentException)
            {
                // The path is invalid
                return;
            }

            watcher.Created += OnChanged;
            watcher.Changed += OnChanged;
            watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite;
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;

            cts = cancellationTokenSource;
            this.ownsCancellationTokenSource = ownsCancellationTokenSource;
        }

        /// <summary>
        /// Get a CancellationToken that is signaled when the shutdown notification is detected.
        /// </summary>
        public CancellationToken Token
        {
            get
            {
                // CancellationToken.None means CanBeCanceled = false, which can facilitate optimizations with tokens.
                return (cts != null) ? cts.Token : CancellationToken.None;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.IndexOf(Path.GetFileName(shutdownFile), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Found the file mark this WebJob as finished
                if (cts != null)
                {
                    cts.Cancel();
                }
            }
        }

        /// <summary>
        /// Stop watching for the shutdown notification
        /// </summary>
        public void Dispose()
        {
            if (watcher != null)
            {
                CancellationTokenSource cts = this.cts;

                if (cts != null && ownsCancellationTokenSource)
                {
                    // Null out the field to prevent a race condition in OnChanged above.
                    this.cts = null;
                    cts.Dispose();
                }

                watcher.Dispose();
                watcher = null;
            }
        }

        internal static WebJobsShutdownWatcher Create(CancellationTokenSource cancellationTokenSource)
        {
            WebJobsShutdownWatcher watcher = new WebJobsShutdownWatcher(cancellationTokenSource, ownsCancellationTokenSource: false);

            if (watcher.watcher == null)
            {
                watcher.Dispose();
                return null;
            }

            return watcher;
        }
    }
}
