using System;

namespace com.github.kbinani.feztradenotify {
    class ApplicationException : Exception {
        public ApplicationException( string message )
            : base( message ) {
        }
    }
}
