using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using Meebey.SmartIrc4net;
using System.Windows.Forms;

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
                        ProcessTradeNotify( window );
                    }
                    while( logStream.HasNext() ) {
                        var line = logStream.Next();
                        Irc.SendMessage( "\x03" + ChatLogLine.GetIrcColorByType( line.Type ) + line.Line + "\x03" );
                        TradeLog.InsertChatLog( DateTime.Now, line.Line, line.Type );
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
        private void ProcessTradeNotify( FEZWindow window ) {
            // トレードを行う
            TradeResult result = null;
            using( var doTradeTask = new DoTradeTask( window ) ) {
                result = doTradeTask.Run();
            }

            // 取引相手の名前を検出
            var customerNameImage = GetCustomerNameImage( result.ScreenShot );
            string strictCustomerName;
            string fuzzyCustomerName;
            GetCustomerName( customerNameImage, out strictCustomerName, out fuzzyCustomerName );

            var replyTask = new ReplyTask( window, result, settings, strictCustomerName, fuzzyCustomerName );
            replyTask.Run();

            TradeLog.Insert( strictCustomerName, result.Time, result.Status );

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
                        case ":stats": {
                            var now = DateTime.Now;
                            if( 4 <= parameters.Length ) {
                                var year = int.Parse( parameters[1] );
                                var month = int.Parse( parameters[2] );
                                var day = int.Parse( parameters[3] );
                                now = new DateTime( year, month, day );
                            }
                            var stats = TradeLog.GetStatistics( now.Year, now.Month, now.Day );
                            Irc.SendNotice( "-------------------------------------" );
                            Irc.SendNotice( "[" + now.Year + "/" + now.Month.ToString( "D2" ) + "/" + now.Day.ToString( "D2" ) + "]" );
                            foreach( var name in stats.Keys ) {
                                var count = stats[name];
                                Irc.SendNotice( name + " : " + count );
                            }
                            Irc.SendNotice( "-------------------------------------" );
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ゲーム画面全体のスクリーンショットから，トレードウィンドウに表示されたトレード相手の名前部分の画像を取得する
        /// 黒いピクセルのみ取り出す
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        private Bitmap GetCustomerNameImage( Bitmap screenShot ) {
            if( screenShot == null ) {
                return null;
            }
            var customerNameGeometry =
                FEZWindow.GetTradeWindowCustomerNameGeometryByWindowGeometry(
                    new Rectangle( 0, 0, screenShot.Width, screenShot.Height ) );
            var result = screenShot.Clone(
                customerNameGeometry,
                screenShot.PixelFormat );

            var letterColor = Color.FromArgb( 255, 0, 0, 0 );
            for( int y = 0; y < result.Height; y++ ) {
                for( int x = 0; x < result.Width; x++ ) {
                    Color color = Color.FromArgb( 255, result.GetPixel( x, y ) );
                    if( color == letterColor ) {
                        result.SetPixel( x, y, letterColor );
                    } else {
                        result.SetPixel( x, y, Color.White );
                    }
                }
            }

            return result;
        }

        private void GetCustomerName( Bitmap customerNameImage, out string strictCustomerName, out string fuzzyCustomerName ) {
            strictCustomerName = "";
            fuzzyCustomerName = "";

            try {
                strictCustomerName = TextFinder.Find( customerNameImage );
            } catch( ApplicationException e ) {
            }

            // 検出結果を描画し、同じになってるか確認する
            var image = (Bitmap)customerNameImage.Clone();
            using( var g = Graphics.FromImage( image ) ) {
                g.FillRectangle( new SolidBrush( Color.FromArgb( 255, Color.White ) ), 0, 0, image.Width, image.Height );
                int letterIndex = 0;
                foreach( var character in strictCustomerName.ToCharArray() ) {
                    int x = TextFinder.DRAW_OFFSET_X + letterIndex * TextFinder.CHARACTER_WIDTH;
                    int y = TextFinder.DRAW_OFFSET_Y;
                    g.DrawString(
                        new string( character, 1 ), TextFinder.GetFont(), new SolidBrush( Color.FromArgb( 255, Color.Black ) ),
                        x, y
                    );
                    if( TextFinder.IsHalfWidthCharacter( character ) ) {
                        letterIndex += 1;
                    } else {
                        letterIndex += 2;
                    }
                }
            }
            WriteLog( customerNameImage, image );

            if( strictCustomerName == "" ) {
                try {
                    fuzzyCustomerName = TextFinder.FuzzyFind( customerNameImage );
                } catch( ApplicationException e ) {
                }
            }

            if( strictCustomerName == "" && fuzzyCustomerName == "" ) {
                WriteLog( customerNameImage );
            }
        }

        /// <summary>
        /// 文字列の判定に失敗したものをログに残す
        /// </summary>
        /// <param name="customerNameImage"></param>
        private void WriteLog( Bitmap customerNameImage, Bitmap detectedResult = null ) {
            var directory = Path.Combine( Path.GetDirectoryName( Application.ExecutablePath ), "reply_task" );
            if( !Directory.Exists( directory ) ) {
                Directory.CreateDirectory( directory );
            }

            string fileName = DateTime.Now.ToFileTime().ToString();
            customerNameImage.Save( Path.Combine( directory, fileName + ".source.png" ), ImageFormat.Png );
            if( detectedResult != null ) {
                detectedResult.Save( Path.Combine( directory, fileName + ".detected.png" ), ImageFormat.Png );
            }
        }
    }
}
