using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace com.github.kbinani.feztradebot {
    class Program {
        static Queue<string> commands = new Queue<string>();
        static string buffer = "";
        static bool stopRequied = false;

        static void Main( string[] args ) {
            TextFinder.Initialize();
            RuntimeSettings settings = new RuntimeSettings( args );

            var keyListener = new Thread( new ThreadStart( ReceiveKeyPress ) );
            keyListener.Start();

            var runner = new DaemonRunner( settings );
            var t = new Thread( new ThreadStart( runner.Run ) );
            t.Start();
            t.Join();

            Console.WriteLine( "hit any key to exit" );
            stopRequied = true;
        }

        public static string PopCommand() {
            lock( commands ) {
                if( 0 < commands.Count ) {
                    return commands.Dequeue();
                } else {
                    return "";
                }
            }
        }

        static void ReceiveKeyPress() {
            while( !stopRequied ) {
                ConsoleKeyInfo info = Console.ReadKey( false );
                if( info.Key == ConsoleKey.Enter ) {
                    lock( commands ) {
                        if( buffer != "" && commands.Count == 0 ) {
                            commands.Enqueue( buffer );
                        }
                        buffer = "";
                        Console.WriteLine();
                    }
                } else {
                    buffer += info.KeyChar;
                }
            }
        }
    }
}
