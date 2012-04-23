using System;

namespace com.github.kbinani.feztradebot {
    class ApplicationException : Exception {
        public ApplicationException( string message )
            : base( message ) {
        }
    }
}
