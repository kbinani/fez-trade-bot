using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;

namespace com.github.kbinani.feztradebot {
    class DaemonRunner {
        enum Status {
            RUNNING,
            PAUSING,
        }

        private RuntimeSettings settings;
        private Status status = Status.PAUSING;

        public DaemonRunner( RuntimeSettings settings ) {
            this.settings = settings;
            this.status = Status.RUNNING;
        }

        public void Run() {
            FEZWindow window = null;
            string playerName = "";
            int sleepSeconds = 1;
            int heartBeatIntervalSeconds = 600;
            int sleepCounter = 1;

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
                }

                Thread.Sleep( 1000 );
                if( this.status == Status.PAUSING ) {
                    continue;
                }
                if( window == null ) {
                    try {
                        IntPtr handle = FEZWindow.GetClientWindow();
                        if( handle == IntPtr.Zero ) {
                            Console.WriteLine( "FEZの画面が見つからなかった" );
                            continue;
                        }
                        window = CreateWindow( handle, out playerName );
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
                    if( window.HasTradeIcon( screenShot ) ) {
                        ProcessTradeNotify( window, screenShot, playerName );
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
        private void ProcessTradeNotify( FEZWindow window, Bitmap screenShot, string playerName ) {
            // トレードを行う
            TradeResult result = null;
            using( var doTradeTask = new DoTradeTask( window ) ) {
                result = doTradeTask.Run();
            }

            var replyTask = new ReplyTask( window, result, settings, playerName );
            replyTask.Run();

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
        private FEZWindow CreateWindow( IntPtr handle, out string playerName ) {
            var result = new FEZWindow( handle );

            var task = new GetNameTask( result );
            task.Run();
            playerName = task.PlayerName;

            return result;
        }
    }
}
