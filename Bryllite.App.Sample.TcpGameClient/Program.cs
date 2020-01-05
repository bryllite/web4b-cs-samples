using Bryllite.Utils.NabiLog;
using System;

namespace Bryllite.App.Sample.TcpGameClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // set log filter
            Log.Filter = LogLevel.All;

            // app run
            new GameClientApp(args) { console = true }.Run();
        }
    }
}
