using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEZTradeBot {
    class Program {
        static void Main( string[] args ) {
            using( var client = new VMultiKeyboardClient() ) {
                client.ClearKey();
            }
        }
    }
}
