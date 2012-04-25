using System;
using System.IO;
using System.Drawing.Imaging;

namespace com.github.kbinani.feztradebot {
    class LoggingTask {
        private TradeResult tradeResult;
        private RuntimeSettings settings;

        public LoggingTask( TradeResult tradeResult, RuntimeSettings settings ) {
            this.tradeResult = tradeResult;
            this.settings = settings;
        }

        public void Run() {
            string timestampString = tradeResult.Time.ToString( "yyyy-MM-dd" + "_" + @"HH\h" + @"mm\m" + @"ss.ff\s" );

            // スクリーンショットを保存
            string imageFileName = timestampString + ".png";
            string imageDirectory = Path.Combine( settings.LogDirectory, "images" );
            if( !Directory.Exists( imageDirectory ) ) {
                Directory.CreateDirectory( imageDirectory );
            }
            string subDirectory = Path.Combine( imageDirectory, tradeResult.Time.ToString( "yyyy-MM-dd" ) );
            if( !Directory.Exists( subDirectory ) ) {
                Directory.CreateDirectory( subDirectory );
            }
            string imageFilePath = Path.Combine( subDirectory, imageFileName );
            tradeResult.ScreenShot.Save( imageFilePath, ImageFormat.Png );

            // ログファイルをcsvで保存
            string logFileName = tradeResult.Time.ToString( "yyyy-MM-dd" ) + ".csv";
            string logFilePath = Path.Combine( settings.LogDirectory, logFileName );

            using( StreamWriter writer = new StreamWriter( logFilePath, true ) ) {
                string line = "";
                line += "\"" + timestampString + "\",";
                line += "\"" + tradeResult.Status + "\",";
                line += "\"" + tradeResult.Message + "\"";
                writer.WriteLine( line );
            }
        }
    }
}
