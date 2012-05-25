using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FEZTradeBot {
    public class RuntimeSettings {
        private string logDirectory = "";
        private string fezLauncher = "";
        private string loginId = "";
        private string loginPassword = "";
        private string loginCharacterName = "";
        private List<string> tellMessageInventoryNoSpace = new List<string>();
        private List<string> tellMessageSucceeded = new List<string>();
        private List<string> tellMessageWeiredItemEntried = new List<string>();
        private string adminPC = "";
        private string ircHost = "";
        private int ircPort = 6667;
        private string ircPassword = "";
        private string ircChannelName = "#feztradebot";
        private Random random = new Random();
        private string sqlHost = "localhost";
        private string sqlUser = "";
        private string sqlPassword = "";

        public RuntimeSettings( string[] args ) {
            using( StreamReader reader = new StreamReader( "fez-trade-bot.conf" ) ) {
                string line;
                while( (line = reader.ReadLine()) != null ) {
                    string[] parameters = line.Split( '=' );
                    if( 2 <= parameters.Length ) {
                        string key = parameters[0];
                        string value = parameters[1];
                        string fieldName = GetFieldName( key );
                        var type = this.GetType();
                        var fieldInfo = type.GetField( fieldName, BindingFlags.Instance | BindingFlags.NonPublic );
                        if( fieldInfo == null ) {
                            continue;
                        }
                        if( fieldInfo.FieldType == typeof( List<string> ) ) {
                            var list = (List<string>)fieldInfo.GetValue( this );
                            list.Add( value );
                            fieldInfo.SetValue( this, list );
                        } else if( fieldInfo.FieldType == typeof( int ) ) {
                            fieldInfo.SetValue( this, int.Parse( value ) );
                        } else if( fieldInfo.FieldType == typeof( string ) ) {
                            fieldInfo.SetValue( this, value );
                        }
                    }
                }
            }
        }

        protected RuntimeSettings() {
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

        public string LoginCharacterName {
            get {
                return loginCharacterName;
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

        public string AdminPC {
            get {
                return adminPC;
            }
        }

        public string IrcHost {
            get {
                return ircHost;
            }
        }

        public int IrcPort {
            get {
                return ircPort;
            }
        }

        public string IrcPassword {
            get {
                return ircPassword;
            }
        }

        public string IrcChannelName {
            get {
                return ircChannelName;
            }
        }

        public string SqlHost {
            get {
                return sqlHost;
            }
        }

        public string SqlUser {
            get {
                return sqlUser;
            }
        }

        public string SqlPassword {
            get {
                return sqlPassword;
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
            int index = random.Next( messageCandidates.Count );
            return messageCandidates[index];
        }

        /// <summary>
        /// confファイルのキー名を、このクラスのフィールド名に変換する
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string GetFieldName( string key ) {
            var parameters = key.Split( '.' );
            for( int i = 1; i < parameters.Length; i++ ) {
                var parameter = parameters[i];
                var c = new string( parameter[0], 1 ).ToUpper();
                parameter = c + parameter.Substring( 1 );
                parameters[i] = parameter;
            }

            var result = "";
            foreach( var parameter in parameters ) {
                result += parameter;
            }
            return result;
        }
    }
}
