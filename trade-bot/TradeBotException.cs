using System;

namespace FEZTradeBot {
    public class TradeBotException : Exception {
        public TradeBotException( string message )
            : base( message ) {
        }
    }
}
