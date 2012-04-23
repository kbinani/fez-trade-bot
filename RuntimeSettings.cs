using System.IO;

namespace com.github.kbinani.feztradenotify {
    class RuntimeSettings {
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
                            case "logDirectory": {
                                logDirectory = value;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public string LogDirectory {
            get {
                return logDirectory;
            }
        }
    }
}
