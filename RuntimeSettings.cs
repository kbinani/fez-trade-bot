using System;
using System.Collections.Generic;
using System.IO;

namespace com.github.kbinani.feztradebot {
    class RuntimeSettings {
        private string logDirectory = "";
        private string fezLauncher = "";
        private string loginId = "";
        private string loginPassword = "";
        private List<string> tellMessageInventoryNoSpace = new List<string>();
        private List<string> tellMessageSucceeded = new List<string>();
        private List<string> tellMessageWeiredItemEntried = new List<string>();
        private Random random = new Random();

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
                            case "tellMessage.inventoryNoSpace": {
                                tellMessageInventoryNoSpace.Add( value );
                                break;
                            }
                            case "tellMessage.succeeded": {
                                tellMessageSucceeded.Add( value );
                                break;
                            }
                            case "tellMessage.weiredItemEntried": {
                                tellMessageWeiredItemEntried.Add( value );
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

        public string TellMessageInventoryNoSpace {
            get {
                return SelectRandomMessage( tellMessageInventoryNoSpace );
            }
        }

        public string TellMessageSucceeded {
            get {
                return SelectRandomMessage( tellMessageSucceeded );
            }
        }

        public string TellMessageWeiredItemEntried {
            get {
                return SelectRandomMessage( tellMessageWeiredItemEntried );
            }
        }

        /// <summary>
        /// リストの中からランダムな位置の文字列を返す
        /// </summary>
        /// <param name="messageCandidates"></param>
        /// <returns></returns>
        private string SelectRandomMessage( List<string> messageCandidates ) {
            if( messageCandidates.Count == 0 ) {
                return "";
            }
            int index = random.Next( messageCandidates.Count - 1 );
            return messageCandidates[index];
        }
    }
}
