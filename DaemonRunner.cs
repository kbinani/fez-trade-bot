using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using Growl.Connector;
using Growl.CoreLibrary;

namespace com.github.kbinani.feztradenotify {
    class DaemonRunner {
        private RuntimeSettings settings;

        public DaemonRunner( RuntimeSettings settings ) {
            this.settings = settings;
        }

        public void Run() {
            FEZWindow window = null;
            while( true ) {
                GC.Collect();
                Thread.Sleep( 1000 );
                if( window == null ) {
                    try {
                        IntPtr handle = WindowsAPI.FindWindow( null, "Fantasy Earth Zero" );
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
                    window = null;
                }
            }
        }

        /// <summary>
        /// トレード枠が来た時の処理を行う
        /// </summary>
        private void ProcessTradeNotify( FEZWindow window, Bitmap screenShot ) {
            // Growly で通知
            Rectangle tradeUserNameRectangle = window.GetTradeUserNameRectangle();
            var tradeUserName = (Bitmap)screenShot.Clone( tradeUserNameRectangle, screenShot.PixelFormat );
            var growlNotifyTask = new GrowlNotifyTask( settings, "", tradeUserName );
            growlNotifyTask.Run();

            var doTradeTask = new DoTradeTask( window, screenShot );
            doTradeTask.Run();

            // ログを出力する
            string fileName = DateTime.Now.ToString( "yyyy-MM-dd" + "_" + @"HH\h" + @"mm\m" + @"ss.ff\s" ) + ".png";
            tradeUserName.Save( Path.Combine( settings.LogDirectory, fileName ), ImageFormat.Png );
        }
    }
}
