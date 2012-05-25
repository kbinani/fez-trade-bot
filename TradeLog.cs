using System;
using System.Text;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace FEZTradeBot {
    class TradeLog {
        const string DATABASE = "fez-trade-bot";
        private static RuntimeSettings settings;
        private static Encoding encoding;

        public static void Init( RuntimeSettings settings ) {
            TradeLog.settings = settings;
            encoding = new UTF8Encoding( false );

            using( var connection = CreateConnection() ) {
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
            using( var connection = CreateConnection() ) {
                var command = new MySqlCommand( sql, connection );
                command.Parameters.AddWithValue( "name", name );
                command.Parameters.AddWithValue( "status", TradeResult.StatusType.SUCCEEDED.ToString() );
                var reader = command.ExecuteReader();
                if( reader.Read() ) {
                    return (DateTime)reader.GetValue( 0 );
                }
            }
            return new DateTime( 1900, 1, 1 );
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
            using( var connection = CreateConnection() ){
                var command = new MySqlCommand( sql, connection );
                command.Parameters.AddWithValue( "targetDay", year + "-" + month.ToString( "D2" ) + "-" + day.ToString( "D2" ) );
                var reader = command.ExecuteReader();
                while( reader.Read() ) {
                    var name = reader.GetString( "name" );
                    var count = reader.GetInt32( "count" );
                    result.Add( name, count );
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
