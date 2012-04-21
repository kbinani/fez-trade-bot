using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;

namespace com.github.kbinani.feztradenotify {
    /// <summary>
    /// FEZのゲーム画面からの情報取得処理と操作処理を行うクラス
    /// </summary>
    class FEZWindow {
        private IntPtr windowHandle;
        private int _width;
        private int _height;

        /// <summary>
        /// FEZのゲーム画面のウィンドウハンドルを指定し，初期化する
        /// </summary>
        /// <param name="handle"></param>
        public FEZWindow( IntPtr handle ) {
            this.windowHandle = handle;

            var geometry = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( handle, ref geometry );
            this._width = geometry.right - geometry.left;
            this._height = geometry.bottom - geometry.top;
        }

        /// <summary>
        /// アイコン領域の画像の中に，トレード要請を表すアイコンが表示されているかどうかを取得する
        /// </summary>
        /// <returns></returns>
        public bool HasTradeIcon( Bitmap screenShot ) {
            Bitmap iconArea = (Bitmap)screenShot.Clone( GetIconAreaRectangle(), screenShot.PixelFormat );
            Bitmap mask = Resource.icon_mask;
            return ImageComparator.Compare( iconArea, Resource.icon_mask );
        }

        /// <summary>
        /// ウィンドウ全体のスクリーンショットから，アイコン領域の部分を取り出す
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public Rectangle GetIconAreaRectangle() {
            int left = this.Width - 105;
            int top = this.Height - 216;
            int width = 97;
            int height = 57;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// トレードウィンドウの，自分のアイテム一欄が表示される領域を取得する
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public Rectangle GetTradeWindowItemAreaGeometry() {
            var result = GetTradeWindowGeometry();

            const int X_OFFSET = 16;
            const int Y_OFFSET = 56;
            const int ICON_AREA_WIDTH = 159;
            const int ICON_AREA_HEIGHT = 255;
            return new Rectangle( result.Left + X_OFFSET, result.Top + Y_OFFSET, ICON_AREA_WIDTH, ICON_AREA_HEIGHT );
        }

        /// <summary>
        /// トレードウィンドウの領域を取得する
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public Rectangle GetTradeWindowGeometry() {
            const int TRADE_WINDOW_WIDTH = 434;
            int x = this.Width / 2 - TRADE_WINDOW_WIDTH / 2;

            const int TRADE_WINDOW_HEIGHT = 368;
            int y = this.Height / 2 - TRADE_WINDOW_HEIGHT / 2;

            return new Rectangle( x, y, TRADE_WINDOW_WIDTH, TRADE_WINDOW_HEIGHT );
        }

        /// <summary>
        /// トレード要求をしてきたユーザー名が表示されている領域を取得する
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public Rectangle GetTradeUserNameRectangle() {
            Rectangle result = GetIconAreaRectangle();
            result.Height = 11;
            return result;
        }

        /// <summary>
        /// トレードウィンドウを閉じるためのボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetTradeWindowCancelButtonPosition() {
            var tradeWindowGeometry = GetTradeWindowGeometry();
            //右下: 729 568
            // ボタン中央 689 536
            int x = tradeWindowGeometry.Right - 40;
            int y = tradeWindowGeometry.Bottom - 32;
            return new Point( x, y );
        }

        /// <summary>
        /// トレードウィンドウにて，アイテムをトレード候補に登録するための「エントリー」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetTradeWindowEntryButtonPosition() {
            var tradeWindowGeometry = GetTradeWindowGeometry();
            // トレードウィンドウ右下: x=729, y=568
            // ボタン中央: x=562, y=536
            int x = tradeWindowGeometry.Right - 167;
            int y = tradeWindowGeometry.Bottom - 32;
            return new Point( x, y );
        }

        /// <summary>
        /// トレードウィンドウにて，トレードを完了させるための「完了」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetTradeWindowOkButtonPosition() {
            var tradeWindowGeometry = GetTradeWindowGeometry();
            // ゲーム画面に対して，
            // トレードウィンドウ右下: x=729, y=568
            // 完了ボタン中央: x=625, y=536
            int x = tradeWindowGeometry.Right - 104;
            int y = tradeWindowGeometry.Bottom - 32;
            return new Point( x, y );
        }

        /// <summary>
        /// トレードウィンドウの，取引相手の名前が描画された領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetTradeWindowCustomerNameGeometry() {
            var tradeWindowGeometry = GetTradeWindowGeometry();
            // トレードウィンドウに対して
            // 左上: x=318, y=40
            // 右下: x=419, y=55
            int left = tradeWindowGeometry.Left + 318;
            int top = tradeWindowGeometry.Top + 40;
            const int width = 419 - 318;
            const int height = 55 - 40;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// トレードウィンドウの，自キャラの名前が描画された領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetTradeWindowNameGeometry() {
            var tradeWindowGeometry = GetTradeWindowGeometry();
            // トレードウィンドウに対して
            // 左上: x=205, y=40
            // 右下: x=306, y=55
            int left = tradeWindowGeometry.Left + 205;
            int top = tradeWindowGeometry.Top + 40;
            const int width = 306 - 205;
            const int height = 55 - 40;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// トレードの際，相手のカバンがいっぱいの場合にその旨メッセージがポップアップする．
        /// このメッセージウィンドウの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetTradeErrorDialogGeometry() {
            // ウィンドウサイズ 1024*768の場合に
            // ダイアログ左上: x=352, y=344
            // ダイアログ右下: x=672, y=424
            const int TRADE_ERROR_DIALOG_WIDTH = 672 - 352;
            const int TRADE_ERROR_DIALOG_HEIGHT = 424 - 344;
            int left = this.Width / 2 - TRADE_ERROR_DIALOG_WIDTH / 2;
            int top = this.Height / 2 - TRADE_ERROR_DIALOG_HEIGHT / 2;
            return new Rectangle( left, top, TRADE_ERROR_DIALOG_WIDTH, TRADE_ERROR_DIALOG_HEIGHT );
        }

        /// <summary>
        /// トレードの際，相手のカバンがいっぱいの場合にその旨メッセージがポップアップする．
        /// このウィンドウを閉じるための「OK」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetTradeErrorDialogOKButtonPosition() {
            var dialogGeometry = GetTradeErrorDialogGeometry();
            // ウィンドウに対して
            // ボタンの右下: x=230, y=64
            // ボタンの左上: x=90, y=48
            return new Point( dialogGeometry.Left + 160, dialogGeometry.Top + 56 );
        }

        /// <summary>
        /// ゲーム画面右下にある「アイテム」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetItemButtonPosition() {
            // ウィンドウサイズ 1024*768の場合に
            // ボタン左上: x=920, y=613
            // ボタン右下: x=1012, y=636
            // なのでボタン中央: x=966, y=625
            int x = this.Width - 58;
            int y = this.Height - 143;
            return new Point( x, y );
        }

        /// <summary>
        /// インベントリの「ソート」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetInventorySortButtonPosition() {
            // インベントリの左上に対して:
            // ボタン中央: x=96, y=356
            var inventoryGeometry = GetInventoryGeometry();
            int x = inventoryGeometry.Left + 96;
            int y = inventoryGeometry.Top + 356;
            return new Point( x, y );
        }

        /// <summary>
        /// インベントリの「閉じる」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetInventoryCloseButtonPosition() {
            // インベントリの左上に対して:
            // ボタン中央: x=96, y=376
            var inventoryGeometry = GetInventoryGeometry();
            int x = inventoryGeometry.Left + 96;
            int y = inventoryGeometry.Top + 376;
            return new Point( x, y );
        }

        /// <summary>
        /// インベントリウィンドウの領域を取得する
        /// </summary>
        /// <returns></returns>
        private Rectangle GetInventoryGeometry() {
            // ウィンドウサイズが1024*768の時，
            // 左上: x=780, y=4
            // 右下: x=972, y=408
            int left = this.Width - 244;
            int top = 4;
            const int INVENTORY_WIDTH = 192;
            const int INVENTORY_HEIGHT = 404;
            return new Rectangle( left, top, INVENTORY_WIDTH, INVENTORY_HEIGHT );
        }

        /// <summary>
        /// ゲームウィンドウ全体の画像を取得する
        /// </summary>
        /// <returns></returns>
        public Bitmap CaptureWindow() {
            IntPtr winDC = WindowsAPI.GetWindowDC( this.windowHandle );
            WindowsAPI.RECT winRect = new WindowsAPI.RECT();
            if( !WindowsAPI.GetWindowRect( this.windowHandle, ref winRect ) ) {
                throw new ApplicationException( "ウィンドウサイズを取得できなかった" );
            }
            Bitmap bmp = new Bitmap( winRect.right - winRect.left,
                winRect.bottom - winRect.top );

            Graphics g = Graphics.FromImage( bmp );
            IntPtr hDC = g.GetHdc();
            WindowsAPI.BitBlt( hDC, 0, 0, bmp.Width, bmp.Height,
                winDC, 0, 0, WindowsAPI.SRCCOPY );

            g.ReleaseHdc( hDC );
            g.Dispose();
            WindowsAPI.ReleaseDC( this.windowHandle, winDC );

            return bmp;
        }

        /// <summary>
        /// ゲームウィンドウの指定した領域の画像を取得する
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public Bitmap CaptureWindow( Rectangle area ){
            var result = CaptureWindow();
            return (Bitmap)result.Clone( area, result.PixelFormat );
        }

        /// <summary>
        /// 画面の指定した位置をクリックする．座標には，ゲーム画面に対する相対座標を指定する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Click( Point position ) {
            var geometry = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( this.windowHandle, ref geometry );
            var clickPosition = new Point();
            clickPosition.X = geometry.left + position.X;
            clickPosition.Y = geometry.top + position.Y;
            WindowsAPI.SetCursorPos( clickPosition.X, clickPosition.Y );

            WindowsAPI.mouse_event( WindowsAPI.LEFTDOWN, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
            Thread.Sleep( 200 );
            WindowsAPI.mouse_event( WindowsAPI.LEFTUP, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
        }

        /// <summary>
        /// 画面の指定した位置をダブルクリックする．座標には，ゲーム画面に対する相対座標を指定する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void DoubleClick( Point position ) {
            var geometry = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( this.windowHandle, ref geometry );
            var clickPosition = new Point();
            clickPosition.X = geometry.left + position.X;
            clickPosition.Y = geometry.top + position.Y;
            WindowsAPI.SetCursorPos( clickPosition.X, clickPosition.Y );

            WindowsAPI.mouse_event( WindowsAPI.LEFTDOWN, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
            Thread.Sleep( 200 );
            WindowsAPI.mouse_event( WindowsAPI.LEFTUP, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
            Thread.Sleep( 200 );
            WindowsAPI.mouse_event( WindowsAPI.LEFTDOWN, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
            Thread.Sleep( 200 );
            WindowsAPI.mouse_event( WindowsAPI.LEFTUP, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
        }

        /// <summary>
        /// トレードウィンドウ内の，アイテムの位置を順に返す反復子を取得する
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public IEnumerable<Rectangle> GetTradeItemGeometryEnumerator() {
            var tradeAreaGeometry = GetTradeWindowItemAreaGeometry();
            const int ITEM_WIDTH = 32;
            const int ITEM_HEIGHT = 64;
            for( int y = 0; y < 4; y++ ) {
                int top = y * ITEM_HEIGHT + tradeAreaGeometry.Top;
                for( int x = 0; x < 5; x++ ) {
                    int left = x * ITEM_WIDTH + tradeAreaGeometry.Left;
                    yield return new Rectangle( left, top, ITEM_WIDTH - 1, ITEM_HEIGHT - 1 );
                }
            }
        }

        /// <summary>
        /// ゲーム画面のウィンドウハンドルを取得する
        /// </summary>
        public IntPtr Handle {
            get {
                return this.windowHandle;
            }
        }

        public int Width {
            get {
                return _width;
            }
        }

        public int Height {
            get {
                return _height;
            }
        }
    }
}
