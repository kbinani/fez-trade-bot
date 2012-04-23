using System;
using System.Threading;

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
                    var notifyMessage = window.CaptureWindow( window.GetChronicleNotifyMessageGeometry() );
                    if( ImageComparator.Compare( notifyMessage, Resource.chronicle_notify_message ) ) {
                        var okButtonPosition = window.GetChronicleNotifyMessageOkButtonPosition();
                        window.Click( okButtonPosition );
                    }
                }
            }
        }

        public void StopAsync() {
            this.stopRequested = true;
        }
    }
}
