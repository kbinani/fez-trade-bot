using System.IO;
using System.Threading;

namespace com.github.kbinani.feztradenotify {
    class Program {
        static void Main( string[] args ) {
            RuntimeSettings settings = new RuntimeSettings( args );

            var runner = new DaemonRunner( settings.GrowlyHost, settings.GrowlyPass, settings.GrowlyPort );
            var t = new Thread( new ThreadStart( runner.Run ) );
            t.Start();
            t.Join();
        }
    }
}
