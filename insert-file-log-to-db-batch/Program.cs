using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FEZTradeBot;

namespace FEZTradeBot {
    class Program {
        static void Main( string[] args ) {
            var start = DateTime.Parse( args[0] );
            var end = DateTime.Parse( args[1] );
            if( end < start ) {
                Console.WriteLine( "日付の範囲指定が不正" );
                return;
            }

            var settings = new RuntimeSettings( new string[] { } );
            TradeLog.Init( settings );

            var target = start;
            while( target <= end ) {
                ProcessLog( target );
                target = target.AddDays( 1 );
            }
        }

        /// <summary>
        /// 指定した日について、ログファイルの内容をDBに転記する
        /// </summary>
        /// <param name="target"></param>
        static void ProcessLog( DateTime target ) {
            var fileName = target.ToString( "yyyy-MM-dd" ) + ".csv";
            if( !File.Exists( fileName ) ) {
                return;
            }
            using( var reader = new StreamReader( fileName ) ) {
                string line = "";
                while( (line = reader.ReadLine()) != null ) {
                    var parameters = line.Split( ',' );
                    for( int i = 0; i < parameters.Length; i++ ) {
                        parameters[i] = parameters[i].Trim( '"' );
                    }
                    var time = ParseDate( parameters[0] );
                    var status = (TradeResult.StatusType)Enum.Parse( typeof( TradeResult.StatusType ), parameters[1] );
                    var name = parameters[2];
                    TradeLog.Insert( name, time, status );
                }
            }
        }

        static DateTime ParseDate( string dateString ) {
            var dateParameters = dateString.Split( '-', '_', 'h', 'm', 's' );
            var year = int.Parse( dateParameters[0] );
            var month = int.Parse( dateParameters[1] );
            var day = int.Parse( dateParameters[2] );
            var hour = int.Parse( dateParameters[3] );
            var min = int.Parse( dateParameters[4] );
            var secWithMilli = double.Parse( dateParameters[5] );
            var sec = (int)Math.Floor( secWithMilli );
            var milliSec = (int)((secWithMilli - sec) * 1000);
            return new DateTime( year, month, day, hour, min, sec, milliSec );
        }
    }
}
