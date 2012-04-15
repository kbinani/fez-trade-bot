using System.IO;
using System.Threading;

namespace com.github.kbinani.feztradenotify {
    class Program {
        static void Main( string[] args ) {
            string host = "localhost";
            string pass = "";
            int port = 23053;
            using( StreamReader reader = new StreamReader( "fez-trade-notify.conf" ) ) {
                string line;
                while( (line = reader.ReadLine()) != null ) {
                    string[] parameters = line.Split( '=' );
                    if( 2 <= parameters.Length ) {
                        string key = parameters[0];
                        string value = parameters[1];
                        switch( key ) {
                            case "host": {
                                host = value;
                                break;
                            }
                            case "pass": {
                                pass = value;
                                break;
                            }
                            case "port": {
                                port = int.Parse( value );
                                break;
                            }
                        }
                    }
                }
            }
            var runner = new DaemonRunner( host, pass, port );
            var t = new Thread( new ThreadStart( runner.Run ) );
            t.Start();
            t.Join();
        }
    }
}
