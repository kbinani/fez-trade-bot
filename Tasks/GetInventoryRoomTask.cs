using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace FEZTradeBot {
    /// <summary>
    /// インベントリの空きを検出するタスク
    /// </summary>
    class GetInventoryRoomTask {
        private FEZWindow window;

        public GetInventoryRoomTask( FEZWindow window ) {
            this.window = window;
        }

        public int Run() {
            // インベントリを開ける
            window.Activate();

            var itemButtonPosition = window.GetItemButtonPosition();
            window.Click( itemButtonPosition );
            Thread.Sleep( TimeSpan.FromSeconds( 2 ) );

            // 空き個数が表示されている領域の画像を取得
            var roomImage = window.GetInventoryRoomTextArea();
            var image = window.CaptureWindow( roomImage );

            // 描画されている判定
            int width = image.Width / 2;
            int height = image.Height;
            var digit10Image = image.Clone( new Rectangle( 0, 0, width, height ), image.PixelFormat );
            var digit1Image = image.Clone( new Rectangle( width, 0, width, height ), image.PixelFormat );
            int room = GetDigit( digit10Image ) * 10 + GetDigit( digit1Image );

            // インベントリを閉じる
            var closeButtonPosition = window.GetInventoryCloseButtonPosition();
            window.Click( closeButtonPosition );
            Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );

            return room;
        }

        private int GetDigit( Bitmap digitImage ) {
            var number = new Bitmap[] {
                Resource.number0,
                Resource.number1,
                Resource.number2,
                Resource.number3,
                Resource.number4,
                Resource.number5,
                Resource.number6,
                Resource.number7,
                Resource.number8,
                Resource.number9,
            };
            for( int digit = 0; digit < 10; ++digit ) {
                if( ImageComparator.CompareStrict( digitImage, number[digit] ) ) {
                    return digit;
                }
            }
            return 0;
        }
    }
}