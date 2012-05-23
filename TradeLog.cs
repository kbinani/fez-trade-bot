using System;
using System.Text;
using MySql.Data.MySqlClient;

namespace FEZTradeBot {
    class TradeLog {
        const string DATABASE = "fez-trade-bot";
        private static RuntimeSettings settings;
        private static MySqlConnection connection;
        private static Encoding encoding;

        public static void Init( RuntimeSettings settings ) {
            TradeLog.settings = settings;
            encoding = new UTF8Encoding( false );

            var config = string.Format(
                "server={0};user id={1}; password={2}; database=" + DATABASE + "; pooling=false",
                settings.SqlHost, settings.SqlUser, settings.SqlPassword );
            connection = new MySqlConnection( config );
            connection.Open();

            var command = new MySqlCommand( @"
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
            command.ExecuteNonQuery();
        }

        public static void Insert( string name, DateTime time, TradeResult.StatusType status ) {
            var sql = "insert into trade_log ( name, time, status ) values( @name, @time, @status )";
            var command = new MySqlCommand( sql, connection );
            command.Parameters.AddWithValue( "name", name );
            command.Parameters.AddWithValue( "time", time );
            command.Parameters.AddWithValue( "status", status.ToString() );
            command.ExecuteNonQuery();
        }

        public static DateTime GetLastTradeTime( string name ) {
            var sql = "select time from trade_log where name = @name and status = @status order by time desc limit 1";
            var command = new MySqlCommand( sql, connection );
            command.Parameters.AddWithValue( "name", name );
            command.Parameters.AddWithValue( "status", TradeResult.StatusType.SUCCEEDED.ToString() );
            var reader = command.ExecuteReader();
            if( reader.Read() ) {
                return (DateTime)reader.GetValue( 0 );
            } else {
                return new DateTime( 1900, 1, 1 );
            }
        }
    }
}
