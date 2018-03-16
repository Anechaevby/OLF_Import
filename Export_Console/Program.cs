using System;
using System.Threading;
using NLog;

namespace Export_Console
{
    public class Program
    {
        private static Mutex _mt;
        private static bool _mutexWasCreated;
        private static Logger Log => LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            _mt = new Mutex(false, "0BB615A1-3501-42D5-B4B6-6ABB51140A62", out _mutexWasCreated);
            if (!_mutexWasCreated)
            {
                return;
            }

            var re = new RunExport();
            try
            {
                re.Execute();
            }
            catch (Exception exc)
            {
                Log.Error(exc);
            }
            finally
            {
                if (_mt != null && _mutexWasCreated)
                {
                    _mt.Dispose();
                }
            }
        }
    }
}
