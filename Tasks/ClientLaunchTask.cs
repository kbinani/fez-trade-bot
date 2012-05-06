using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace FEZTradeBot {
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
                if( ImageComparator.Compare( login, Resource.login ) ) {
                    break;
                }
                window.Click( new Point( x, y ) );
            }

            // ログインID入力
            //TODO: 大文字・が来た時の処理
            //TODO: 既に入っているログインIDを消去する処理
            //TODO: ログインIDを保存する，のオプションのチェックを外した状態にする
            window.Click( window.GetLoginDialogIDPosition() );
            foreach( char c in settings.LoginId.ToUpper().ToCharArray() ) {
                WindowsAPI.keybd_event( (byte)c, 0, 0, UIntPtr.Zero );
                WindowsAPI.keybd_event( (byte)c, 0, WindowsAPI.KEYEVENTF_KEYUP, UIntPtr.Zero );
            }
            Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );

            // ログインPASS入力
            window.Click( window.GetLoginDialogPasswordPosition() );
            foreach( char c in settings.LoginPassword.ToUpper().ToCharArray() ) {
                WindowsAPI.keybd_event( (byte)c, 0, 0, UIntPtr.Zero );
                WindowsAPI.keybd_event( (byte)c, 0, WindowsAPI.KEYEVENTF_KEYUP, UIntPtr.Zero );
            }
            Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );

            // ログインボタンを押す
            window.Click( window.GetLoginDialogLoginButtonPosition() );

            // キャラクタ選択ダイアログが表示されるまで待つ
            var characterSelectDialog = window.GetCharacterSelectDialogGeometry();
            while( !ImageComparator.Compare( window.CaptureWindow( characterSelectDialog ), Resource.character_select_dialog ) ) {
                // ログインボタン押し下げ後、何らかのお知らせダイアログが表示されることがあるので、
                // 「閉じる」ボタンが見つからなくなるまで押し続ける
                try {
                    while( true ) {
                        var position = FindCloseButton( window );
                        window.Click( position );
                    }
                } catch( ApplicationException e ) { }
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            }

            // 指定したキャラクタ名がダイアログに表示されるまで、別キャラクタを表示させる
            try {
                while( true ) {
                    var characterName = GetCharacterName( window );
                    if( characterName == settings.LoginCharacterName ) {
                        break;
                    }
                    window.Click( window.GetCharacterSelectNextRightPosition() );
                    Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
                }
            } catch( ApplicationException e ) {
                Console.WriteLine( "ClientLaunchTaskの例外: " + e.Message );
            }

            // ログインボタンを押す
            window.Click( window.GetCharacterSelectButtonPosition() );
            Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            window.Click( window.GetCharacterSelectConfirmDialogOKButtonPosition() );
            Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );

            // エイケルナル大陸をクリック
            // 初心者云々のダイアログが表示されている可能性があるので、2回クリックする

            // アズルウッド首都をクリック

            // フィールドインボタンをクリック
        }

        /// <summary>
        /// キャラクタ選択ダイアログから、キャラクタ名を取得する
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        private string GetCharacterName( FEZWindow window ) {
            var characterNameGeometry = window.GetCharacterSelectDialogNameGeometry();
            var characterNameRawImage = window.CaptureWindow( characterNameGeometry );
            var characterNameImage = TextFinder.CreateFilteredImage( characterNameRawImage, Color.Black );
            return TextFinder.Find( characterNameImage );
        }

        /// <summary>
        /// 閉じるボタン
        /// </summary>
        /// <returns></returns>
        private Point FindCloseButton( FEZWindow window ) {
            var screenImage = window.CaptureWindow();
            var screenWidth = screenImage.Width;
            var screenHeight = screenImage.Height;
            var screen = GetColorArray( screenImage );

            var maskImage = Resource.close_button;
            int maskWidth = maskImage.Width;
            int maskHeight = maskImage.Height;
            var mask = GetColorArray( maskImage );
            var maskTransparentColor = mask[0, 0];

            for( int offsetY = 0; offsetY < screenHeight - maskHeight; offsetY++ ) {
                for( int offsetX = 0; offsetX < screenWidth - maskWidth; offsetX++ ) {
                    bool match = true;
                    for( int y = 0; y < maskHeight; y++ ) {
                        for( int x = 0; x < maskWidth; x++ ) {
                            var maskColor = mask[x, y];
                            if( maskColor == maskTransparentColor ) {
                                continue;
                            }
                            var screenColor = screen[x + offsetX, y + offsetY];
                            if( maskColor != screenColor ) {
                                match = false;
                                break;
                            }
                        }
                        if( !match ) {
                            break;
                        }
                    }

                    if( match ) {
                        int x = offsetX + maskWidth / 2;
                        int y = offsetY + maskHeight / 2;
                        return new Point( x, y );
                    }
                }
            }
            throw new ApplicationException( "閉じるボタンを見つけられなかった" );
        }

        private static Color[,] GetColorArray( Bitmap image ) {
            int width = image.Width;
            int height = image.Height;
            var result = new Color[width, height];
            for( int y = 0; y < height; y++ ) {
                for( int x = 0; x < width; x++ ) {
                    result[x, y] = Color.FromArgb( 255, image.GetPixel( x, y ) );
                }
            }
            return result;
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
