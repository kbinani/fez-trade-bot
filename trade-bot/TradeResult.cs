using System;
using System.Drawing;

namespace FEZTradeBot {
    /// <summary>
    /// トレードの実行結果を表現するクラス
    /// </summary>
    public class TradeResult {
        public enum StatusType {
            /// <summary>
            /// 取引成功
            /// </summary>
            SUCCEEDED,

            /// <summary>
            /// 取引失敗
            /// </summary>
            FAILED,

            /// <summary>
            /// 相手のインベントリが満タン
            /// </summary>
            INVENTRY_NO_SPACE,

            /// <summary>
            /// 相手が野獣の血以外のアイテムを渡してきた
            /// </summary>
            WEIRED_ITEM_ENTRIED,

            /// <summary>
            /// トレード相手によるキャンセル処理
            /// </summary>
            CANCELLED_BY_CUSTOMER,

            /// <summary>
            /// 売り切れ
            /// </summary>
            SOLD_OUT,
        }

        private StatusType status;
        private DateTime time;
        private Bitmap screenShot;

        public TradeResult( StatusType status, Bitmap screenShot ) {
            this.status = status;
            this.time = DateTime.Now;
            this.screenShot = screenShot;
        }

        public StatusType Status {
            get {
                return status;
            }
            set {
                status = value;
            }
        }

        public DateTime Time {
            get {
                return time;
            }
        }

        public Bitmap ScreenShot {
            get {
                return screenShot;
            }
        }
    }
}
