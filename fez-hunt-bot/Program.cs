using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;

namespace FEZTradeBot {
    class DoClickWorker {
        private Point position;
        private bool isOn;
        private FEZWindow window;

        public DoClickWorker( FEZWindow window ) {
            this.window = window;
            this.isOn = false;
        }

        public void SetPosition( Point position ) {
            this.position = position;
        }

        public void Stop() {
            isOn = false;
        }

        public void Resume() {
            isOn = true;
        }

        public void Run() {
            while( true ) {
                if( isOn ) {
                    window.Click( position );
                }
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            }
        }
    }

    class Program {
        static void Main( string[] args ) {
            FEZWindow window = null;

            window = new FEZWindow( FEZWindow.GetClientWindow(), false );
            var clickWorker = new DoClickWorker( window );
            var thread = new Thread( new ThreadStart( clickWorker.Run ) );
            thread.Start();

            while( true ) {
                GC.Collect();
                Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );
                Bitmap screen = null;
                try {
                    screen = window.CaptureWindow();
                } catch( Exception e ) {
                    Console.Error.WriteLine( e.Message );
                    window.Dispose();
                    window = null;
                    window = new FEZWindow( FEZWindow.GetClientWindow(), false );
                    continue;
                }
                int width = 100;
                int height = 100;
                int top = screen.Height / 2 - height;
                int left = screen.Width / 2 - width / 2;
                var mobArea = new Rectangle( left, top, width, height );
                var mobAreaImage = screen.Clone( mobArea, screen.PixelFormat );
                var total = 0;
                var match = 0;
                foreach( var point in PixelEnumerator.GetEnumerable( mobAreaImage.Width, mobAreaImage.Height ) ) {
                    var color = mobAreaImage.GetPixel( point.X, point.Y );
                    if( color.R == 0 &&
                        3 <= color.G && color.G <= 148 &&
                        18 <= color.B && color.B <= 159 ) {
                        match++;
                    }
                    total++;
                }
                var percentage = match / (double)total * 100.0;
                if( 1.0 <= percentage ) {
                    var clickPosition = new Point( mobArea.Left + mobArea.Width / 2, mobArea.Top + mobArea.Height / 2 );
                    clickWorker.SetPosition( clickPosition );
                    clickWorker.Resume();
                } else {
                    clickWorker.Stop();
                }
                Console.WriteLine( (match / total * 100) + "% [" + match + "/" + total + "] " + DateTime.Now );
            }
        }
    }
}

