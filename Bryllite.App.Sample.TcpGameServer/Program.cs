using Bryllite.Utils.NabiLog;
using System;

namespace Bryllite.App.Sample.TcpGameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // set log filter
            Log.Filter = LogLevel.All;

            // app run
            new GameServerApp(args).Run();
        }
    }
}
