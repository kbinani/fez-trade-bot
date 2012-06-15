using System;

namespace FEZTradeBot {
    public class FEZBotException : Exception {
        public FEZBotException( string message )
            : base( message ) {
        }
    }
}
