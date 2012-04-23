using System;
using System.Drawing;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// トレードの実行結果を表現するクラス
    /// </summary>
    class TradeResult {
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
        }

        private StatusType status;
        private DateTime time;
        private Bitmap screenShot;
        private string message;

        public TradeResult( StatusType status, DateTime time, Bitmap screenShot, string message ) {
            this.status = status;
            this.time = time;
            this.screenShot = screenShot;
            this.message = message;
        }

        public StatusType Status {
            get {
                return status;
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

        public string Message {
            get {
                return message;
            }
        }
    }
}
