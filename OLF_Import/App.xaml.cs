using System;
using System.Threading;
using System.Windows;
using NLog;

namespace OLF_Import
{
    public partial class App : Application
    {
        private Mutex _mt;
        public static Logger log;
        private bool _mutexWasCreated;

        protected override void OnStartup(StartupEventArgs e)
        {
            log = LogManager.GetCurrentClassLogger();

            log.Trace("Version: {0}", Environment.Version);
            log.Trace("OS: {0}", Environment.OSVersion);
            log.Trace("Command: {0}", Environment.CommandLine);
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            this._mt = new Mutex(false, "0BB615A1-3507-42D3-B4B6-6ABB51140A60", out _mutexWasCreated);
            if (!_mutexWasCreated)
            {
                Application.Current.Shutdown();
                return;
            }
            base.OnStartup(e);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            log.Error(e.Exception);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mt != null && _mutexWasCreated)
            {
                _mt.Dispose();
            }
            base.OnExit(e);
        }
    }
}
