using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using Growl.Connector;
using Growl.CoreLibrary;

namespace com.github.kbinani.feztradenotify {
    class DaemonRunner {
        private const string APPLICATION_NAME = "FEZ trade notify";

        private GrowlConnector connector = null;
        private Growl.Connector.Application application = null;
        private NotificationType notificationType;

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

                try {
                    window.GetTradeIconLocation();
                    ProcessTradeNotify( window );
                } catch( ApplicationException e ) {
                    Console.WriteLine( e.Message );
                    window = null;
                }
            }
        }

        /// <summary>
        /// トレード枠が来た時の処理を行う
        /// </summary>
        private void ProcessTradeNotify( FEZWindow window ) {
            // Growly で通知
            Bitmap iconArea = window.GetTradeIcon();
            SendNotify( iconArea );

            // ログを出力する
            string fileName = DateTime.Now.ToString( "yyyy-MM-dd" + "_" + @"HH\h" + @"mm\m" + @"ss.ff\s" ) + ".png";
            iconArea.Save( fileName, ImageFormat.Png );
        }

        private GrowlConnector GetConnector() {
            if( connector == null ) {
                connector = new GrowlConnector( settings.GrowlyPass, settings.GrowlyHost, settings.GrowlyPort );

                application = new Growl.Connector.Application( APPLICATION_NAME );
                notificationType = new NotificationType( "FEZ_TRADE_NOTIFICATION", "Trade Notification" );
                connector.Register( application, new NotificationType[] { notificationType } );

                connector.EncryptionAlgorithm = Cryptography.SymmetricAlgorithmType.PlainText;
            }
            return connector;
        }

        private void SendNotify( Bitmap screenShot ) {
            var connector = GetConnector();
            CallbackContext callbackContext = new CallbackContext( "some fake information", "fake data" );

            Notification notification = new Notification(
                application.Name, notificationType.Name, DateTime.Now.Ticks.ToString(),
                "Trade Notification", "trade request received", screenShot, false, Priority.Normal, "0" );
            connector.Notify( notification, callbackContext );
        }
    }
}
