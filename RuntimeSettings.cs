using System.IO;

class RuntimeSettings {
    private string host = "localhost";
    private string pass = "";
    private int port = 23053;

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
                    }
                }
            }
        }
    }

    public string GrowlyHost {
        get {
            return host;
        }
    }

    public string GrowlyPass {
        get {
            return pass;
        }
    }

    public int GrowlyPort {
        get {
            return port;
        }
    }
}
