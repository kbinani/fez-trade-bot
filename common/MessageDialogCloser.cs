using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

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
                Thread.Sleep( TimeSpan.FromSeconds( 10 ) );
                lock( window ) {
                    try {
                        CheckChronicleNotifyDialog();
                        CheckRoyMessageDialog();
                        CheckNetworkErrorDialog();
                        CheckClientException();
                    } catch( CommonException e ) {
                        Console.WriteLine( e.Message );
                    }
                }
            }
        }

        public void StopAsync() {
            this.stopRequested = true;
        }

        private void CheckClientException() {
            var dialogHandle = WindowsAPI.FindWindow( "#32770", "FEzero_Client.exe" );
            if( dialogHandle == IntPtr.Zero ) {
                return;
            }
            var buttonHandle = WindowsAPI.FindWindowEx( dialogHandle, IntPtr.Zero, null, "送信しない(&D)" );
            var geometry = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( buttonHandle, ref geometry );
            int x = (geometry.left + geometry.right) / 2;
            int y = (geometry.top + geometry.bottom) / 2;
            WindowsAPI.SetForegroundWindow( dialogHandle );
            Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );
            FEZWindow.DoClick( x, y );
        }

        /// <summary>
        /// ネットワークエラーのダイアログをチェックする
        /// </summary>
        private void CheckNetworkErrorDialog() {
            var dialogGeometry = window.GetNetworkErrorDialogGeoemtry();
            var dialogImage = window.CaptureWindow( dialogGeometry );
            if( ImageComparator.Compare( dialogImage, Resource.network_error_dialog ) ) {
                // エラーダイアログのOKボタンを押す
                var position = window.GetNetworkErrorDialogOKButtonPosition();
                window.Click( position );
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );

                // 確認ダイアログが出るまで、exitボタンを押し続ける
                var exitButtonGeometry = window.GetLoginExitButtonGeometry();
                int x = exitButtonGeometry.Left + exitButtonGeometry.Width / 2;
                int y = exitButtonGeometry.Top + exitButtonGeometry.Height / 2;
                var exitButtonPosition = new Point( x, y );
                var confirmDialogGeometry = window.GetLogoutDialogGeometry();
                while( true ) {
                    var confirmDialogImage = window.CaptureWindow( confirmDialogGeometry );
                    if( ImageComparator.Compare( confirmDialogImage, Resource.logout_confirm_dialog, 0 ) ) {
                        break;
                    }
                    window.Click( exitButtonPosition );
                    Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
                }

                // ゲームクライアントが消えるまで、確認ダイアログのOKボタンを押し続ける
                var okButtonPosition = window.GetLogoutDialogOKButtonPosition();
                while( true ) {
                    var handle = FEZWindow.GetClientWindow();
                    if( handle == IntPtr.Zero ) {
                        break;
                    }
                    window.Click( okButtonPosition );
                    Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
                }
            }
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
