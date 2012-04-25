using System;
using System.Threading;
using System.Drawing;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// クロニクルの任務破棄ダイアログを閉じるタスク
    /// </summary>
    class ChronicleNotifyCloser {
        private FEZWindow window;
        private bool stopRequested;

        public ChronicleNotifyCloser( FEZWindow window ) {
            this.window = window;
            this.stopRequested = false;
        }

        public void Run() {
            while( !this.stopRequested ) {
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
                lock( window ) {
                    var notifyMessageGeometry = window.GetChronicleNotifyMessageGeometry();
                    Bitmap notifyMessage = null;
                    try {
                        notifyMessage = window.CaptureWindow( window.GetChronicleNotifyMessageGeometry() );
                    } catch( ApplicationException e ) {
                        Console.Error.WriteLine( e.Message );
                        break;
                    }

                    if( !ImageComparator.Compare( notifyMessage, Resource.chronicle_notify_message ) ) {
                        continue;
                    }
                    
                    var okButtonPosition = window.GetChronicleNotifyMessageOkButtonPosition();
                    try {
                        window.Click( okButtonPosition );
                    } catch( ApplicationException e ) {
                        Console.Error.WriteLine( e.Message );
                        break;
                    }
                }
            }
        }

        public void StopAsync() {
            this.stopRequested = true;
        }
    }
}
