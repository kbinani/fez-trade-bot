using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Meebey.SmartIrc4net;

namespace FEZTradeBot {
    class Program {
        static Queue<string> commands = new Queue<string>();
        static string buffer = "";
        static bool stopRequied = false;

        static void Main( string[] args ) {
            RuntimeSettings settings = new RuntimeSettings( args );

            TextFinder.Initialize();
            Irc.Start( settings );

            var keyListener = new Thread( new ThreadStart( ReceiveKeyPress ) );
            keyListener.Start();

            var runner = new DaemonRunner( settings );
            var t = new Thread( new ThreadStart( runner.Run ) );
            t.Start();
            t.Join();

            Irc.Stop();
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

        public static void PushCommand( string command ) {
            lock( commands ) {
                commands.Enqueue( command );
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
