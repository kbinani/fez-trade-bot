using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace com.github.kbinani.feztradebot {
    class ClientLaunchTask : IDisposable {
        private RuntimeSettings settings;
        private bool stopRequested = false;

        public ClientLaunchTask( RuntimeSettings settings ) {
            this.settings = settings;
        }

        public void Run() {
            var handle = StartClient();
            using( var window = new FEZWindow( handle ) ) {
                Login( window );
            }
        }

        public void Dispose() {
            stopRequested = true;
        }

        /// <summary>
        /// クライアントのログイン処理を行う
        /// </summary>
        private void Login( FEZWindow window ) {
            // ログインウィンドウが出現するまで，startボタンの位置をクリックし続ける
            var startButtonGeometry = window.GetLoginStartButtonGeometry();
            int x = startButtonGeometry.Left + startButtonGeometry.Width / 2;
            int y = startButtonGeometry.Top + startButtonGeometry.Height / 2;

            var loginGeometry = window.GetLoginDialogGeometry();
            while( stopRequested == false ) {
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
                var login = window.CaptureWindow( loginGeometry );
                {//TODO:
                    login.Save( "login_captured.png", ImageFormat.Png );
                }
                if( ImageComparator.Compare( login, Resource.login ) ) {
                    break;
                }
                window.Click( new Point( x, y ) );
            }
        }

        /// <summary>
        /// ランチャーを起動し，ランチャーの「START」ボタンを押すことでクライアント本体を起動する
        /// </summary>
        private IntPtr StartClient() {
            Process process = new Process();
            process.StartInfo.FileName = settings.FezLauncher;
            process.Start();

            // ランチャーを起動
            IntPtr updater = IntPtr.Zero;
            while( updater == IntPtr.Zero && stopRequested == false ) {
                updater = FEZWindow.GetLauncherWindow();
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            }
            if( updater == IntPtr.Zero ) {
                throw new ApplicationException( "クライアント・ランチャが起動できなかった" );
            }

            // ランチャーの「START」ボタンを押す
            IntPtr startButton = WindowsAPI.FindWindowEx( updater, IntPtr.Zero, "BUTTON", "START" );
            WindowsAPI.RECT startButtonGeometry = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( startButton, ref startButtonGeometry );
            var x = (startButtonGeometry.left + startButtonGeometry.right) / 2;
            var y = (startButtonGeometry.top + startButtonGeometry.bottom) / 2;
            FEZWindow.DoClick( x, y );

            // 起動するのを待つ
            var client = IntPtr.Zero;
            while( client == IntPtr.Zero && stopRequested == false ) {
                client = FEZWindow.GetClientWindow();
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            }
            return client;
        }
    }
}
