using System;

namespace FEZTradeBot {
    public class CommonException : Exception {
        public CommonException( string message )
            : base( message ) {
        }
    }
}
