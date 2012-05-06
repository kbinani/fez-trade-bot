using System;
using System.Threading;

namespace FEZTradeBot {
    /// <summary>
    /// インベントリの中身をソートするタスク
    /// </summary>
    class SortInventoryTask {
        private FEZWindow window;

        public SortInventoryTask( FEZWindow window ) {
            this.window = window;
        }

        public void Run() {
            // インベントリを開いて，ソートする
            var itemButtonPosition = window.GetItemButtonPosition();
            window.Click( itemButtonPosition );
            Thread.Sleep( TimeSpan.FromSeconds( 2 ) );

            var sortButtonPosition = window.GetInventorySortButtonPosition();
            window.Click( sortButtonPosition );
            Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );

            var closeButtonPosition = window.GetInventoryCloseButtonPosition();
            window.Click( closeButtonPosition );
            Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
        }
    }
}
