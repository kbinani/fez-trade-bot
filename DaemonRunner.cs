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

        private string host;
        private string pass;
        private int port;

        private FEZWindow window = null;

        public DaemonRunner( string host, string pass, int port ) {
            this.host = host;
            this.pass = pass;
            this.port = port;

            IntPtr handle = WindowsAPI.FindWindow( null, "Fantasy Earth Zero" );
            this.window = new FEZWindow( handle );
        }

        public void Run() {
            while( true ) {
                try {
                    this.window.GetTradeIconLocation();
                    Bitmap iconArea = this.window.GetTradeIcon();
                    SendNotify( iconArea );
                } catch( ApplicationException e ) {
                }
                GC.Collect();
                Thread.Sleep( 1000 );
            }
        }

        private GrowlConnector GetConnector() {
            if( connector == null ) {
                connector = new GrowlConnector( this.pass, this.host, this.port );

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
