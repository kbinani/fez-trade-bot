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
                        window = new FEZWindow( handle );
                    } catch( ApplicationException e ) {
                        Console.WriteLine( e.Message );
                        continue;
                    }
                }

                Bitmap screenShot = null;
                try {
                    screenShot = window.CaptureWindow();
                    if( window.HasTradeIcon( screenShot ) ) {
                        ProcessTradeNotify( window, screenShot );
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

            var replyTask = new ReplyTask( window, result );
            replyTask.Run();

            // ログを出力する
            var loggingTask = new LoggingTask( result, settings );
            loggingTask.Run();
        }
    }
}
