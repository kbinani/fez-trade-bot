using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// FEZのゲーム画面からの情報取得処理と操作処理を行うクラス
    /// </summary>
    class FEZWindow : IDisposable {
        /// <summary>
        /// トレードウィンドウの，トレード相手の名前が表示されているエリアの幅
        /// </summary>
        public const int TRADE_WINDOW_CUSTOMER_GEOMETRY_WIDTH = 419 - 318 - 5;

        /// <summary>
        /// トレードウィンドウの，トレード相手の名前が表示されているエリアの高さ
        /// </summary>
        public const int TRADE_WINDOW_CUSTOMER_GEOMETRY_HEIGHT = 55 - 40 - 3;

        private IntPtr windowHandle;
        private int _width;
        private int _height;
        private ChronicleNotifyCloser closer = null;

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

            this.closer = new ChronicleNotifyCloser( this );
            Thread thread = new Thread( new ThreadStart( this.closer.Run ) );
            thread.Start();
        }

        public static void DoClick( int x, int y ) {
            WindowsAPI.SetCursorPos( x, y );
            WindowsAPI.mouse_event( WindowsAPI.LEFTDOWN, (uint)x, (uint)y, 0, UIntPtr.Zero );
            Thread.Sleep( 200 );
            WindowsAPI.mouse_event( WindowsAPI.LEFTUP, (uint)x, (uint)y, 0, UIntPtr.Zero );
        }

        /// <summary>
        /// ゲームクライアントのウィンドウハンドルを取得する
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetClientWindow() {
            return WindowsAPI.FindWindow( "MainWindow", "Fantasy Earth Zero" );
        }

        /// <summary>
        /// ゲームランチャーのウィンドウハンドルを取得する
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetLauncherWindow() {
            return WindowsAPI.FindWindow( "#32770", "Fantasy Earth Zero" );
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
        /// 左下に表示されるトレード要求アイコン領域のうち，ユーザー名が表示されている領域を取得する
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
            int left = tradeWindowGeometry.Left + 318 + 8;
            int top = tradeWindowGeometry.Top + 40 + 2;
            const int width = TRADE_WINDOW_CUSTOMER_GEOMETRY_WIDTH;
            const int height = TRADE_WINDOW_CUSTOMER_GEOMETRY_HEIGHT;
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
        /// ゲーム画面右下にある「システム」ボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetSystemButtonPosition() {
            // ウィンドウサイズ 1024*768の場合に
            // ボタン左上: x=920, y=688
            // ボタン右下: x=1012, y=711
            int x = this.Width - 58;
            int y = this.Height - 68;
            return new Point( x, y );
        }

        /// <summary>
        /// ゲーム画面右下の「システム」ボタンを押すことで現れるメニューの中の，
        /// 「部隊リスト」メニューのいちを取得する
        /// </summary>
        /// <returns></returns>
        public Point GetSystemGuildListMenuPosition() {
            // ウィンドウサイズが800*600の場合に，
            // 左上: x=552, y=334
            // 右下: x=678, y=350
            int x = this.Width - 185;
            int y = this.Height - 258;
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
        /// クロニクルの任務破棄ダイアログの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetChronicleNotifyMessageGeometry() {
            // ウィンドウサイズが1024*768の時
            // 左上: x=352, y=344
            // 右下: x=672, y=424
            int width = 320;
            int height = 80;
            int left = this.Width / 2 - width / 2;
            int top = this.Height / 2 - height / 2;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// クロニクルの任務破棄ダイアログの，okボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetChronicleNotifyMessageOkButtonPosition() {
            var geometry = GetChronicleNotifyMessageGeometry();
            // ダイアログの左上に対して，
            // ボタン中央: x=160, y=56
            int x = geometry.Left + 160;
            int y = geometry.Top + 56;
            return new Point( x, y );
        }

        /// <summary>
        /// ログイン画面のSTARTボタンの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetLoginStartButtonGeometry() {
            int startButtonLeft = 212 * Width / 800;
            int startButtonRight = 595 * Width / 800;
            int startButtonTop = 420 * Height / 600;

            int exitButtonTop = 450 * Height / 600;
            int height = exitButtonTop - 1 - startButtonTop;
            return new Rectangle( startButtonLeft,
                                  startButtonTop,
                                  startButtonRight - startButtonLeft,
                                  height );
        }

        /// <summary>
        /// ログイン画面のEXITボタンの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetLoginExitButtonGeometry() {
            var startButtonGeometry = GetLoginStartButtonGeometry();
            int left = startButtonGeometry.Left;
            int top = startButtonGeometry.Bottom + 1;
            return new Rectangle( left, top, startButtonGeometry.Width, startButtonGeometry.Height );
        }

        /// <summary>
        /// ID とパスワードを入力するダイアログの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetLoginDialogGeometry() {
            //1024*768の時，Rectangle( 384, 456, 248, 148 )
            //800*600の時，Rectangle( 272, 372, 248, 148 )
            //1152*864の時，Rectangle( 448, 504, 248, 148 )
            const int TOP_OFFSET = 72;
            const int LEFT_OFFSET = 4;
            const int WIDTH = 248;
            const int HEIGHT = 148;

            int top = this.Height / 2 + TOP_OFFSET;
            int left = this.Width / 2 - WIDTH / 2 - LEFT_OFFSET;
            return new Rectangle( left, top, WIDTH, HEIGHT );
        }

        /// <summary>
        /// ログインダイアログの，ID入力欄の位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetLoginDialogIDPosition() {
            var loginDialogGeometry = GetLoginDialogGeometry();
            // ログインダイアログに対して，
            // 左上: x=112, y=32
            // 右下: x=223, y=47
            int x = loginDialogGeometry.Left + 167;
            int y = loginDialogGeometry.Top + 39;
            return new Point( x, y );
        }

        /// <summary>
        /// ログインダイアログの，パスワード入力欄の位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetLoginDialogPasswordPosition() {
            var loginDialogGeometry = GetLoginDialogGeometry();
            // ログインダイアログに対して，
            // 左上: x=112, y=72
            // 右下: x=223, y=87
            int x = loginDialogGeometry.Left + 167;
            int y = loginDialogGeometry.Top + 79;
            return new Point( x, y );
        }

        /// <summary>
        /// キャラクタ選択ダイアログの、キャラクタ情報を表示しているダイアログの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetCharacterSelectDialogGeometry() {
            // 1024*768のとき、
            // 左上: x=16, y=64
            // 右下: x=224, y=320
            const int width = 224 - 16;
            const int height = 320 - 64;
            return new Rectangle( 16, 64, width, height );
        }

        /// <summary>
        /// 部隊リストダイアログの領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetGuildListDialogGeometry() {
            // 800*600の時，
            // 左上: x=16, y=104
            // 右下: x=784, y=496
            int width = 768;
            int height = 392;
            int left = this.Width / 2 - width / 2;
            int top = this.Height / 2 - height / 2;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// 部隊リストに表示されている index 番目のメンバーの領域を取得する
        /// index は 0 から始まる．返す座標は，ゲーム画面左上に対して
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Rectangle GetGuildListDialogMemberGeometry( int index ) {
            var guildListGeometry = GetGuildListDialogGeometry();//746,92
            // 0 番目の領域: { left=8, top=76, width=738, height=16 }; ダイアログ左上に対して
            // 1 番目の領域: { left=8, top=94, width=738, height=16 }; ダイアログ左上に対して
            int x = 8;
            int y = 76 + index * 18;
            return new Rectangle(
                guildListGeometry.Left + x, guildListGeometry.Top + y,
                738, 16 );
        }

        /// <summary>
        /// 部隊リストに表示されているメンバーをクリックした時に出現する，「Tellを送信」ボタンの領域を取得する
        /// </summary>
        /// <param name="clickPosition">クリックした座標．座標はゲーム画面左上に対して</param>
        /// <returns>領域．座標はゲーム画面左上に対して</returns>
        public Rectangle GetGuildListDialogMemberSendTellMenuGeometry( Point clickPosition ) {
            // クリック位置: x=1120, y=306 の時，
            // 左上: x=1128, y=310
            // 右下: x=1188, y=322
            const int width = 1188 - 1128;
            const int height = 322 - 310;
            const int deltaX = 1128 - 1120;
            const int deltaY = 310 - 306;
            return new Rectangle(
                clickPosition.X + deltaX, clickPosition.Y + deltaY,
                width, height );
        }

        /// <summary>
        /// チャット入力欄の領域を取得する
        /// </summary>
        /// <returns></returns>
        public Rectangle GetChatTextBoxGeometry() {
            // 800 * 600 の時，
            // 左上: x=36, y=574
            // 右下: x=295, y=586
            int width = 295 - 36;
            int height = 586 - 574;
            return new Rectangle(
                this.Width + 36, this.Height - 26,
                width, height );
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
        public Bitmap CaptureWindow( Rectangle area ) {
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

            DoClick( clickPosition.X, clickPosition.Y );
        }

        /// <summary>
        /// 画面の指定した位置をダブルクリックする．座標には，ゲーム画面に対する相対座標を指定する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void DoubleClick( Point position ) {
            Click( position );
            Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            Click( position );
        }

        /// <summary>
        /// トレードウィンドウ内の，アイテムの位置を順に返す反復子を取得する
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public IEnumerable<Rectangle> GetTradeItemGeometryEnumerator() {
            var tradeAreaGeometry = GetTradeWindowItemAreaGeometry();
            return GetItemGeometryEnumerator( tradeAreaGeometry.Location, 5, 4 );
        }

        /// <summary>
        /// トレード相手がエントリーしたアイテム位置を順に返す反復子を取得する
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Rectangle> GetTradeCustomerEntriedItemGeometryEnumerator() {
            var tradeWindow = GetTradeWindowGeometry();
            // トレードウィンドウに対して
            // 左上: x=321, y=56
            int x = tradeWindow.Left + 321;
            int y = tradeWindow.Top + 56;
            return GetItemGeometryEnumerator( new Point( x, y ), 3, 3 );
        }

        public void Dispose() {
            this.closer.StopAsync();
        }

        /// <summary>
        /// 左上隅の座標と，アイテム枠の縦横の個数を指定することで，
        /// アイテム枠の領域を順に返す反復子を取得する
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="columnCount"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private IEnumerable<Rectangle> GetItemGeometryEnumerator( Point topLeft, int columnCount, int rowCount ) {
            const int ITEM_WIDTH = 32;
            const int ITEM_HEIGHT = 64;
            for( int y = 0; y < rowCount; y++ ) {
                int top = y * ITEM_HEIGHT + topLeft.Y;
                for( int x = 0; x < columnCount; x++ ) {
                    int left = x * ITEM_WIDTH + topLeft.X;
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

        /// <summary>
        /// ウィンドウをアクティブ化する
        /// </summary>
        public void Activate() {
            WindowsAPI.SetForegroundWindow( this.Handle );
        }
    }
}
