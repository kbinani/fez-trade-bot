using System.IO;

namespace com.github.kbinani.feztradenotify {
    class RuntimeSettings {
        private string host = "localhost";
        private string pass = "";
        private int port = 23053;
        private string logDirectory = "";

        public RuntimeSettings( string[] args ) {
            using( StreamReader reader = new StreamReader( "fez-trade-notify.conf" ) ) {
                string line;
                while( (line = reader.ReadLine()) != null ) {
                    string[] parameters = line.Split( '=' );
                    if( 2 <= parameters.Length ) {
                        string key = parameters[0];
                        string value = parameters[1];
                        switch( key ) {
                            case "growlHost": {
                                host = value;
                                break;
                            }
                            case "growlServerPass": {
                                pass = value;
                                break;
                            }
                            case "growlPort": {
                                port = int.Parse( value );
                                break;
                            }
                            case "logDirectory": {
                                logDirectory = value;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public string GrowlHost {
            get {
                return host;
            }
        }

        public string GrowlPass {
            get {
                return pass;
            }
        }

        public int GrowlPort {
            get {
                return port;
            }
        }

        public string LogDirectory {
            get {
                return logDirectory;
            }
        }
    }
}
