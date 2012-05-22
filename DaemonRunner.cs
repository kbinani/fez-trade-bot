using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using Meebey.SmartIrc4net;

namespace FEZTradeBot {
    class DaemonRunner {
        enum Status {
            RUNNING,
            PAUSING,
        }

        private RuntimeSettings settings;
        private Status status = Status.PAUSING;
        private FEZWindow window = null;

        public DaemonRunner( RuntimeSettings settings ) {
            this.settings = settings;
            this.status = Status.RUNNING;
            Irc.OnRawMessage += new IrcEventHandler( Irc_OnRawMessage );
        }

        public void Run() {
            int sleepSeconds = 1;
            int heartBeatIntervalSeconds = 600;
            int sleepCounter = 1;
            ChatLogStream logStream = null;

            while( true ) {
                GC.Collect();

                // コマンドが入力されていたら状態を変更する
                string command = Program.PopCommand();
                if( command == "quit" ) {
                    if( window != null ) {
                        window.Dispose();
                    }
                    break;
                } else if( command == "pause" ) {
                    this.status = Status.PAUSING;
                } else if( command == "resume" ) {
                    this.status = Status.RUNNING;
                } else if ( command == "capture" ) {
                    window.CaptureWindow().Save( "capture_" + DateTime.Now.ToString( "yyyy-MM-dd" + "_" + @"HH\h" + @"mm\m" + @"ss.ff\s" ) + ".png", ImageFormat.Png );
                } else if( command == "help" ) {
                    Console.WriteLine( "available commands:" );
                    Console.WriteLine( "    capture  take a screen shot" );
                    Console.WriteLine( "    help     show command help" );
                    Console.WriteLine( "    pause    pause monitoring game window" );
                    Console.WriteLine( "    quit     terminate this program" );
                    Console.WriteLine( "    resume   resume monitoring game window" );
                    Console.WriteLine( "    reset-mapcapture" );
                    Console.WriteLine( "             マップ画像の不透明な部分を抽出する処理をやり直す" );
                }

                Thread.Sleep( 1000 );
                if( this.status == Status.PAUSING ) {
                    continue;
                }
                if( window == null ) {
                    try {
                        IntPtr handle = FEZWindow.GetClientWindow();
                        if( handle == IntPtr.Zero ) {
                            var clientLaunchTask = new ClientLaunchTask( settings );
                            clientLaunchTask.Run();
                            continue;
                        }
                        window = CreateWindow( handle, out logStream );
                    } catch( ApplicationException e ) {
                        Console.WriteLine( e.Message );
                        continue;
                    }
                }
                sleepCounter--;
                if( sleepCounter == 0 ) {
                    Irc.SendNotice( "heart beat message, time: " + DateTime.Now + ", window: " + (window == null ? "DEAD" : "ALIVE") );
                    sleepCounter = heartBeatIntervalSeconds / sleepSeconds;
                }

                Bitmap screenShot = null;
                try {
                    screenShot = window.CaptureWindow();
                    logStream.PushScreenShot( screenShot );
                    if( window.HasTradeIcon( screenShot ) ) {
                        ProcessTradeNotify( window, screenShot );
                    }
                    while( logStream.HasNext() ) {
                        var line = logStream.Next();
                        Irc.SendMessage( "\x03" + ChatLogLine.GetIrcColorByType( line.Type ) + line.Line + "\x03" );
                    }
                } catch( ApplicationException e ) {
                    Console.WriteLine( e.Message );
                    window.Dispose();
                    window = null;
                }
            }
        }

        /// <summary>
        /// トレード枠が来た時の処理を行う
        /// </summary>
        private void ProcessTradeNotify( FEZWindow window, Bitmap screenShot ) {
            // トレードを行う
            TradeResult result = null;
            using( var doTradeTask = new DoTradeTask( window ) ) {
                result = doTradeTask.Run();
            }

            var replyTask = new ReplyTask( window, result, settings );
            replyTask.Run();

            TradeLog.Insert( result.Message, result.Time, result.Status );

            if( result.Status == TradeResult.StatusType.SUCCEEDED ) {
                var sortInventoryTask = new SortInventoryTask( window );
                sortInventoryTask.Run();
            }

            // ログを出力する
            var loggingTask = new LoggingTask( result, settings );
            loggingTask.Run();
        }

        /// <summary>
        /// FEZWindow のインスタンスを作成する
        /// </summary>
        /// <returns></returns>
        private FEZWindow CreateWindow( IntPtr handle, out ChatLogStream logStream ) {
            var result = new FEZWindow( handle );

            logStream = new ChatLogStream( result );

            return result;
        }

        private void Irc_OnRawMessage( object sender, Meebey.SmartIrc4net.IrcEventArgs e ) {
            if( e.Data.Type == ReceiveType.ChannelMessage &&
                false == e.Data.Nick.StartsWith( Irc.NICK ) && e.Data.Message.StartsWith( ":" ) ) {
                var message = e.Data.Message;
                var parameters = message.Split( ' ' );
                if( 0 < parameters.Length ) {
                    var command = parameters[0].ToLower();
                    switch( command ) {
                        case ":sendmessage": {
                            var argument = message.Substring( command.Length ).Trim();
                            if( argument.StartsWith( "/" ) && window != null ) {
                                window.SendMessage( argument );
                            }
                            break;
                        }
                        case ":capture": {
                            Program.PushCommand( "capture" );
                            break;
                        }
                    }
                }
            }
        }
    }
}
