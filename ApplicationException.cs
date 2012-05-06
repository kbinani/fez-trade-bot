using System;

namespace FEZTradeBot {
    class ApplicationException : Exception {
        public ApplicationException( string message )
            : base( message ) {
        }
    }
}
