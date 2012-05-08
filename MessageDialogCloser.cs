using System;
using System.Threading;
using System.Drawing;

namespace FEZTradeBot {
    /// <summary>
    /// いろんなダイアログを閉じるタスク
    /// </summary>
    class MessageDialogCloser {
        private FEZWindow window;
        private bool stopRequested;

        public MessageDialogCloser( FEZWindow window ) {
            this.window = window;
            this.stopRequested = false;
        }

        public void Run() {
            while( !this.stopRequested ) {
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
                lock( window ) {
                    try {
                        CheckChronicleNotifyDialog();
                        CheckRoyMessageDialog();
                    } catch( ApplicationException e ) {
                        Console.WriteLine( e.Message );
                        break;
                    }
                }
            }
        }

        public void StopAsync() {
            this.stopRequested = true;
        }

        /// <summary>
        /// 首都にフィールドインした直後に表示される、ロイのメッセージダイアログを閉じる
        /// </summary>
        private void CheckRoyMessageDialog() {
            var royMessageGeometry = window.GetRoyMessageGeometry();
            var royMessageTitleGeometry = royMessageGeometry;
            royMessageTitleGeometry.Height = Resource.roy_message_header.Height;
            var royMessageTitleImage = window.CaptureWindow( royMessageTitleGeometry );
            if( ImageComparator.Compare( royMessageTitleImage, Resource.roy_message_header, 0 ) ) {
                int x = royMessageTitleGeometry.Left + royMessageTitleGeometry.Width / 2;
                int y = royMessageTitleGeometry.Top + royMessageTitleGeometry.Height / 2;
                window.Click( new Point( x, y ) );
            }
        }

        /// <summary>
        /// クロニクルの、戦場が成立しなかったダイアログがあるかどうか確認し、あれば閉じる
        /// </summary>
        private void CheckChronicleNotifyDialog() {
            var notifyMessageGeometry = window.GetChronicleNotifyMessageGeometry();
            var notifyMessage = window.CaptureWindow( window.GetChronicleNotifyMessageGeometry() );
            if( ImageComparator.Compare( notifyMessage, Resource.chronicle_notify_message ) ) {
                var okButtonPosition = window.GetChronicleNotifyMessageOkButtonPosition();
                window.Click( okButtonPosition );
            }
        }
    }
}
