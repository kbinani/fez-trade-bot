using System;
using System.Text;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace FEZTradeBot {
    public class TradeLog {
        const string DATABASE = "fez-trade-bot";
        private static RuntimeSettings settings;
        private static Encoding encoding;

        public static void Init( RuntimeSettings settings ) {
            TradeLog.settings = settings;
            encoding = new UTF8Encoding( false );

            MySqlConnection connection = null;
            try {
                connection = CreateConnection();
                var createTradeLogTable = new MySqlCommand( @"
CREATE TABLE IF NOT EXISTS `trade_log` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `name` varchar(32) NOT NULL,
  `time` datetime NOT NULL,
  `status` varchar(40) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_name` (`name`),
  KEY `idx_time` (`time`),
  KEY `idx_status` (`status`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8", connection );
                createTradeLogTable.ExecuteNonQuery();

                var createTradeStatsExcludeUsersTable = new MySqlCommand( @"
CREATE TABLE IF NOT EXISTS `trade_stats_exclude_users` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `name` varchar(32) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;", connection );
                createTradeStatsExcludeUsersTable.ExecuteNonQuery();

                var createChatLogTable = new MySqlCommand( @"
CREATE TABLE IF NOT EXISTS `chat_log` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `time` datetime NOT NULL,
  `message` varchar(128) NOT NULL,
  `type` varchar(16) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_time` (`time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;", connection );
                createChatLogTable.ExecuteNonQuery();
            } catch( Exception e ) {
                Console.Error.WriteLine( e.Message );
            } finally {
                if( connection != null ) {
                    connection.Dispose();
                }
            }
        }

        public static void InsertChatLog( DateTime time, string message, ChatLogLine.LineType status ) {
            var sql = "insert into chat_log ( time, message, type ) values( @time, @message, @status )";
            MySqlConnection connection = null;
            try {
                connection = CreateConnection();
                var command = new MySqlCommand( sql, connection );
                command.Parameters.AddWithValue( "time", time );
                command.Parameters.AddWithValue( "message", message );
                command.Parameters.AddWithValue( "status", status.ToString() );
                command.ExecuteNonQuery();
            } catch( Exception e ) {
                Console.Error.WriteLine( e.Message );
            } finally {
                if( connection != null ) {
                    connection.Dispose();
                }
            }
        }

        public static void Insert( string name, DateTime time, TradeResult.StatusType status ) {
            var sql = "insert into trade_log ( name, time, status ) values( @name, @time, @status )";
            MySqlConnection connection = null;
            try {
                connection = CreateConnection();
                var command = new MySqlCommand( sql, connection );
                command.Parameters.AddWithValue( "name", name );
                command.Parameters.AddWithValue( "time", time );
                command.Parameters.AddWithValue( "status", status.ToString() );
                command.ExecuteNonQuery();
            } catch( Exception e ) {
                var message = "取引ログを記録できなかった。name=" + name + "; time=" + time + "; status=" + status.ToString();
                Console.Error.WriteLine( message );
                Irc.SendMessage( message );
            } finally {
                if( connection != null ) {
                    connection.Dispose();
                }
            }
        }

        public static DateTime GetLastTradeTime( string name ) {
            var sql = "select time from trade_log where name = @name and status = @status order by time desc limit 1";
            MySqlConnection connection = null;
            try {
                connection = CreateConnection();
                var command = new MySqlCommand( sql, connection );
                command.Parameters.AddWithValue( "name", name );
                command.Parameters.AddWithValue( "status", TradeResult.StatusType.SUCCEEDED.ToString() );
                var reader = command.ExecuteReader();
                if( reader.Read() ) {
                    return (DateTime)reader.GetValue( 0 );
                }
            } catch( Exception e ) {
                Console.Error.WriteLine( e.Message );
            } finally {
                if( connection != null ) {
                    connection.Dispose();
                }
            }
            return new DateTime( 1900, 1, 1 );
        }

        /// <summary>
        /// 指定した日が含まれる週の取引情報サマリーを取得する
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static Dictionary<DateTime, Tuple<int, int>> GetWeeklyStatistics( int year, int month, int day, out Tuple<int, int> weekly ) {
            var targetDay = new DateTime( year, month, day );
            var sunday = targetDay.AddDays( -(int)targetDay.DayOfWeek );

            var result = new Dictionary<DateTime, Tuple<int, int>>();
            foreach( var dayOfWeek in Enum.GetValues( typeof( DayOfWeek ) ) ) {
                var target = sunday.AddDays( (int)dayOfWeek );
                result.Add( target, GetUU( target, target ) );
            }
            
            weekly = GetUU( sunday, sunday.AddDays( Enum.GetValues( typeof( DayOfWeek ) ).Length - 1 ) );

            return result;
        }

        /// <summary>
        /// 指定した期間のUUと取引数を取得する
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static Tuple<int, int> GetUU( DateTime start, DateTime end ) {
            var sql = @"
                select
                    count(distinct name) as uu,
                    count(name) as trade_count
                from
                    (
                        select
                            name
                        from
                            trade_log
                        where
                            name <> ''
                        and @start <= time
                        and time < @nextDayOfEnd
                        and name <> all
                            (
                                select
                                    name
                                from
                                    trade_stats_exclude_users
                            )
                        and status = 'SUCCEEDED'
                    ) as foo
                ;";
            MySqlConnection connection = null;
            try {
                connection = CreateConnection();
                var command = new MySqlCommand( sql, connection );
                var nextDayOfEnd = end.AddDays( 1 );
                command.Parameters.AddWithValue( "start", start.Year + "-" + start.Month + "-" + start.Day );
                command.Parameters.AddWithValue( "nextDayOfEnd", nextDayOfEnd.Year + "-" + nextDayOfEnd.Month + "-" + nextDayOfEnd.Day );
                var reader = command.ExecuteReader();
                if( reader.Read() ) {
                    var dailyUU = reader.GetInt32( "uu" );
                    var dailyCount = reader.GetInt32( "trade_count" );
                    return new Tuple<int, int>( dailyUU, dailyCount );
                }
            } catch( Exception e ) {
                Console.Error.WriteLine( e.Message );
            } finally{
                if( connection != null ) {
                    connection.Dispose();
                }
            }

            return null;
        }

        /// <summary>
        /// 指定した日の取引情報を取得する
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static Dictionary<string, int> GetStatistics( int year, int month, int day ) {
            var result = new Dictionary<string, int>();
            var sql = @"
                select
                    name,
                    count(name) as count
                from
                    trade_log
                where
                    name <> ''
                and status = 'SUCCEEDED'
                and date_format (time, '%Y-%m-%d') = @targetDay
                and name <> all
                    (
                        select
                            name
                        from
                            trade_stats_exclude_users
                    )
                group by
                    name
                order by
                    count desc,
                    name asc;";
            MySqlConnection connection = null;
            try {
                connection = CreateConnection();
                var command = new MySqlCommand( sql, connection );
                command.Parameters.AddWithValue( "targetDay", year + "-" + month.ToString( "D2" ) + "-" + day.ToString( "D2" ) );
                var reader = command.ExecuteReader();
                while( reader.Read() ) {
                    var name = reader.GetString( "name" );
                    var count = reader.GetInt32( "count" );
                    result.Add( name, count );
                }
            } catch( Exception e ) {
                Console.Error.WriteLine( e.Message );
            } finally {
                if( connection != null ) {
                    connection.Dispose();
                }
            }
            return result;
        }

        private static MySqlConnection CreateConnection() {
            var config = string.Format(
                "server={0};user id={1}; password={2}; database=" + DATABASE + "; pooling=false",
                settings.SqlHost, settings.SqlUser, settings.SqlPassword );
            var connection = new MySqlConnection( config );
            connection.Open();
            return connection;
        }
    }
}
