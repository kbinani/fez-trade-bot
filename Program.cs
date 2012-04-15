using System.Threading;

namespace com.github.kbinani.feztradenotify {
    class Program {
        static void Main( string[] args ) {
            var runner = new DaemonRunner();
            var t = new Thread( new ThreadStart( runner.Run ) );
            t.Start();
            t.Join();
        }
    }
}
