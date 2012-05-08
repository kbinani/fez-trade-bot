using System;
using System.Threading;
using Meebey.SmartIrc4net;

namespace FEZTradeBot {
    static class Irc {
        public const string NICK = "bot";

        private static RuntimeSettings settings;
        private static Thread thread;
        private static IrcClient irc;
        private static bool stopRequested = false;

        /// <summary>
        /// irc のメッセージが届いた際に呼ばれる
        /// </summary>
        public static event IrcEventHandler OnRawMessage;

        public static void Start( RuntimeSettings settings ) {
            Irc.settings = settings;
            thread = new Thread( new ThreadStart( Run ) );
            thread.Start();
        }

        public static void Stop() {
            stopRequested = true;
            if( irc != null ) {
                try {
                    irc.Disconnect();
                } catch { }
            }
        }

        public static void SendNotice( string message ) {
            try {
                irc.SendMessage( SendType.Notice, settings.IrcChannelName, message );
            } catch( Exception e ) {
                Console.WriteLine( e.Message );
            }
        }

        public static void SendMessage( string message ) {
            try {
                irc.SendMessage( SendType.Message, settings.IrcChannelName, message );
            } catch( Exception e ) {
                Console.WriteLine( e.Message );
            }
        }

        private static void Run() {
            while( !stopRequested ) {
                irc = new IrcClient();
                irc.Encoding = System.Text.Encoding.UTF8;
                irc.SendDelay = 200;
                irc.ActiveChannelSyncing = true;
                irc.OnRawMessage += new IrcEventHandler( irc_OnRawMessage );
                irc.Connect( settings.IrcHost, settings.IrcPort );
                irc.Login( NICK, NICK, 0, NICK, settings.IrcPassword );
                irc.RfcJoin( settings.IrcChannelName );
                irc.Listen();
                if( irc.IsConnected ) {
                    irc.Disconnect();
                }
                if( !stopRequested ) {
                    Thread.Sleep( TimeSpan.FromSeconds( 10 ) );
                }
            }
        }

        static void irc_OnRawMessage( object sender, IrcEventArgs e ) {
            if( OnRawMessage != null ) {
                OnRawMessage( sender, e );
            }
        }
    }
}
