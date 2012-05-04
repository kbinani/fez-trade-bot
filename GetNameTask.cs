using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// 自分のキャラクタ名を取得するタスク
    /// </summary>
    class GetNameTask {
        private FEZWindow window;
        private string playerName = "";
        private const int CHARACTER_WIDTH = 6;
        private const int CHARACTER_HEIGHT = 12;

        public GetNameTask( FEZWindow window ) {
            this.window = window;
        }

        public void Run() {
            window.Click( window.GetStatusButtonPosition() );
            Thread.Sleep( TimeSpan.FromSeconds( 1 ) );

            var playerNameRawImage = window.CaptureWindow( window.GetStatusDialogPlayerNameGeometry() );
            var playerNameImage = GetPlayerNameImage( playerNameRawImage );
            try {
                playerName = TextFinder.Find( playerNameImage );
            } catch { }

            window.Click( window.GetStatusButtonPosition() );
            Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
        }

        public string PlayerName {
            get {
                return playerName;
            }
        }

        private Bitmap GetPlayerNameImage( Bitmap playerNameRawImage ) {
            var letterColor = Color.FromArgb( 255, Color.Black );
            var backgroundColor = Color.FromArgb( 255, Color.White );

            // 黒いピクセルを文字とみなし、それ以外を白で塗りつぶした画像を作成する
            var filtered = (Bitmap)playerNameRawImage.Clone();
            for( int y = 0; y < playerNameRawImage.Height; y++ ) {
                for( int x = 0; x < playerNameRawImage.Width; x++ ) {
                    var c = Color.FromArgb( 255, playerNameRawImage.GetPixel( x, y ) );
                    if( c == letterColor ) {
                        filtered.SetPixel( x, y, letterColor );
                    } else {
                        filtered.SetPixel( x, y, backgroundColor );
                    }
                }
            }

            // 右詰めで表示されているので、左詰に直す
            var chars = new Bitmap[16];
            for( int i = 0; i < 16; i++ ) {
                chars[i] = new Bitmap( CHARACTER_WIDTH, CHARACTER_HEIGHT );
                int offset = i * CHARACTER_WIDTH;
                for( int y = 0; y < CHARACTER_HEIGHT; y++ ) {
                    for( int x = 0; x < CHARACTER_WIDTH; x++ ) {
                        chars[i].SetPixel( x, y, filtered.GetPixel( x + offset, y ) );
                    }
                }
            }
            int startIndex = -1;
            for( int i = 0; i < 16; i++ ) {
                var charImage = chars[i];
                var allWhite = true;
                for( int x = 0; x < charImage.Width; x++ ) {
                    for( int y = 0; y < charImage.Height; y++ ) {
                        var c = Color.FromArgb( 255, charImage.GetPixel( x, y ) );
                        if( c == letterColor ) {
                            allWhite = false;
                            break;
                        }
                    }
                }
                if( !allWhite ) {
                    startIndex = i;
                    break;
                }
            }
            if( startIndex < 0 ) {
                throw new ApplicationException( "ステータスダイアログから名前を取得できなかった" );
            }
            var result = new Bitmap( filtered.Width, filtered.Height, playerNameRawImage.PixelFormat );
            using( var g = Graphics.FromImage( result ) ) {
                g.FillRectangle( new SolidBrush( backgroundColor ), 0, 0, result.Width, result.Height );
            }
            for( int i = startIndex; i < 16; i++ ) {
                int offset = (i - startIndex) * CHARACTER_WIDTH;
                var charImage = chars[i];
                for( int x = 0; x < charImage.Width; x++ ) {
                    for( int y = 0; y < charImage.Height; y++ ) {
                        result.SetPixel( x + offset, y, charImage.GetPixel( x, y ) );
                    }
                }
            }
            return result;
        }
    }
}
