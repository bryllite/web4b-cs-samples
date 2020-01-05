using System;

namespace Bryllite.App.Sample.GameWalletApp
{
    class Program
    {
        static void Main(string[] args)
        {
            new GameWalletApp(args) { console = true }.Run();
        }
    }
}
