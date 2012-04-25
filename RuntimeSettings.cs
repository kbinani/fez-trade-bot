using System.IO;

namespace com.github.kbinani.feztradebot {
    class RuntimeSettings {
        private string logDirectory = "";
        private string fezLauncher = "";

        public RuntimeSettings( string[] args ) {
            using( StreamReader reader = new StreamReader( "fez-trade-bot.conf" ) ) {
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
                            case "fezLauncher": {
                                fezLauncher = value;
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

        public string FezLauncher {
            get {
                return fezLauncher;
            }
        }
    }
}
