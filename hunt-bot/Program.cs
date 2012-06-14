using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;

namespace FEZTradeBot {
    class Program {
        static void Main( string[] args ) {
            using( var window = new FEZWindow( FEZWindow.GetClientWindow(), false ) ) {
                var worker = new RunningAroundWorker( window );
                worker.Run();

                var attackWorker = new AttackWorker( window );
                attackWorker.Resume();

                var thread = new Thread( new ThreadStart( attackWorker.Run ) );
                thread.Start();
                thread.Join();
            }
        }
    }
}

