using System;

namespace FEZTradeBot {
    public class ApplicationException : Exception {
        public ApplicationException( string message )
            : base( message ) {
        }
    }
}
