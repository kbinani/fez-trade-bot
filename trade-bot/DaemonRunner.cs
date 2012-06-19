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
                } else if ( command == "item_count" ) {
                    if( window != null ) {
                        var task = new GetInventoryRoomTask( window );
                        var itemCount = task.Run();
                        Irc.SendNotice( "item_count=" + itemCount );
                    }
                } else if( command == "help" ) {
                    Console.WriteLine( "available commands:" );
                    Console.WriteLine( "    capture  take a screen shot" );
                    Console.WriteLine( "    help     show command help" );
                    Console.WriteLine( "    pause    pause monitoring game window" );
                    Console.WriteLine( "    quit     terminate this program" );
                    Console.WriteLine( "    resume   resume monitoring game window" );
                    Console.WriteLine( "    item_count" );
                    Console.WriteLine( "             get the number of items in inventory" );
                }

                Thread.Sleep( 1000 );
                if( this.status == Status.PAUSING ) {
                    continue;
                }
                if( window == null ) {
                    try {
                        IntPtr handle = FEZWindow.GetClientWindow();
                        if( handle == IntPtr.Zero ) {
                            var clientLaunchTask = new ClientLaunchTask( settings.LoginId, settings.LoginPassword, settings.LoginCharacterName, settings.FezLauncher );
                            clientLaunchTask.Run();
                            continue;
                        }
                        window = CreateWindow( handle, out logStream );
                    } catch( FEZBotException e ) {
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
                } catch( FEZBotException e ) {
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
            // トレード前のインベントリの空き容量を検出
            var getInventoryRoomTask = new GetInventoryRoomTask( window );
            var inventoryRoomBefore = getInventoryRoomTask.Run();

            // トレードを行う
            TradeResult result = null;
            var doTradeTask = new DoTradeTask( window );
            result = doTradeTask.Run();

            // トレード後のインベントリの空き容量を検出
            var inventoryRoomAfter = getInventoryRoomTask.Run();

            if( inventoryRoomBefore - inventoryRoomAfter == 0 && result.Status == TradeResult.StatusType.SUCCEEDED ) {
                // アイテム個数が減っていないのに、SUCCEEDED 扱いだった場合は
                // 相手のキャンセルによるものと判定する
                result.Status = TradeResult.StatusType.CANCELLED_BY_CUSTOMER;
            }

            // 取引相手の名前を検出
            var customerNameImage = GetCustomerNameImage( result.ScreenShot );
            string strictCustomerName;
            string fuzzyCustomerName;
            GetCustomerName( customerNameImage, out strictCustomerName, out fuzzyCustomerName );

            var replyTask = new ReplyTask( window, result, settings, strictCustomerName, fuzzyCustomerName );
            replyTask.Run();

            TradeLog.Insert( strictCustomerName, result.Time, result.Status );

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
                        case ":weekly_stats": {
                            var now = DateTime.Now;
                            if( 4 <= parameters.Length ) {
                                var year = int.Parse( parameters[1] );
                                var month = int.Parse( parameters[2] );
                                var day = int.Parse( parameters[3] );
                                now = new DateTime( year, month, day );
                            }
                            Tuple<int, int> weekly;
                            var stats = TradeLog.GetWeeklyStatistics( now.Year, now.Month, now.Day, out weekly );
                            var sunday = now.AddDays( -(int)now.DayOfWeek );
                            Irc.SendNotice( "-------------------------------------" );
                            foreach( var day in stats.Keys ) {
                                var dailyStats = stats[day];
                                Irc.SendNotice( day.Month + "/" + day.Day + "; UU:" + dailyStats.Item1 + ", 配布数:" + dailyStats.Item2 );
                            }
                            Irc.SendNotice( "-------------------------------------" );
                            Irc.SendNotice( "週間; UU:" + weekly.Item1 + ", 配布数:" + weekly.Item2 );
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
            } catch( FEZBotException e ) {
            }

            if( strictCustomerName == "" ) {
                try {
                    fuzzyCustomerName = TextFinder.FuzzyFind( customerNameImage );
                } catch( FEZBotException e ) {
                }
            }

            // "MS ゴシック" には、違う文字が同じ形で表示されるものがある。
            // キャラクタ名としてこれらの文字を使っている場合、正しいキャラクタ名を判定できない。たとえば、"―", "─" は同じ表示になる。
            // この文字を使っているキャラクタ"foo―"がいたとして、もうひとつ可能なキャラクタ名"foo─"がいないことがわかっている場合、
            // 読み替えればよい。
            var mappedName = settings.GetActualNameByFuzzyName( fuzzyCustomerName );
            if( mappedName != "" ) {
                strictCustomerName = mappedName;
            }

            // 検出結果を描画し、同じになってるか確認する
            var background = Color.FromArgb( 255, Color.White );
            var image = (Bitmap)customerNameImage.Clone();
            using( var g = Graphics.FromImage( image ) ) {
                g.FillRectangle( new SolidBrush( background ), 0, 0, image.Width, image.Height );
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
            if( !ImageComparator.CompareStrict( customerNameImage, image, background ) ) {
                WriteLog( customerNameImage, image );
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
