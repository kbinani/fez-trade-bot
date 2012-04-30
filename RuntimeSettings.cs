using System.IO;

namespace com.github.kbinani.feztradebot {
    class RuntimeSettings {
        private string logDirectory = "";
        private string fezLauncher = "";
        private string loginId = "";
        private string loginPassword = "";

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
                            case "loginId": {
                                loginId = value;
                                break;
                            }
                            case "loginPassword": {
                                loginPassword = value;
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

        public string LoginId {
            get {
                return loginId;
            }
        }

        public string LoginPassword {
            get {
                return loginPassword;
            }
        }
    }
}
