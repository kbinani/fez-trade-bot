using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace FEZTradeBot {
    /// <summary>
    /// ゲーム画面からチャットログを読み取り、読み取った結果を1行ずつ返す
    /// </summary>
    public class ChatLogStream {
        private FEZWindow window;
        private List<ChatLogLine> buffer;
        private int currentBufferIndex = -1;

        public ChatLogStream( FEZWindow window ) {
            this.window = window;
            buffer = new List<ChatLogLine>();
        }

        /// <summary>
        /// スクリーンショットを push する
        /// </summary>
        /// <param name="screenShot"></param>
        public void PushScreenShot( Bitmap screenShot ) {
            ChatLogLine[] lines = null;
            try {
                lock( buffer ) {
                    lines = GetLines( screenShot );
                    MergeLogLines( lines );
                }
            } catch( FEZBotException e ) {
            }
        }

        /// <summary>
        /// 次の行があるかどうかを取得する
        /// </summary>
        /// <returns></returns>
        public bool HasNext() {
            return currentBufferIndex + 1 < buffer.Count;
        }

        /// <summary>
        /// 次の行を取得する
        /// </summary>
        /// <returns></returns>
        public ChatLogLine Next() {
            var index = currentBufferIndex + 1;
            if( buffer.Count <= index ) {
                throw new FEZBotException( "行データが取得できない" );
            }
            var result = buffer[index];
            currentBufferIndex++;
            return result;
        }

        /// <summary>
        /// バッファーと、スクリーンショットから取得したチャットの行をマージする
        /// </summary>
        /// <param name="lines"></param>
        private void MergeLogLines( ChatLogLine[] lines ) {
            int offset = buffer.Count - lines.Length - 1;
            for( int i = 0; i < lines.Length; i++ ) {
                offset++;
                if( offset < 0 ) {
                    continue;
                }

                // lineBuffer[offset]～が、
                // line[0]～と同じかどうかを調べる
                int bufferIndex = offset;
                int lineIndex = 0;
                bool match = true;
                while( bufferIndex < buffer.Count && lineIndex < lines.Length ) {
                    if( !buffer[bufferIndex].Equals( lines[lineIndex] ) ) {
                        match = false;
                        break;
                    }
                    bufferIndex++;
                    lineIndex++;
                }

                if( match ) {
                    var newLineCount = offset + lines.Length - buffer.Count;
                    for( int j = lines.Length - newLineCount; j < lines.Length; j++ ) {
                        buffer.Add( lines[j] );
                    }
                    return;
                }
            }

            foreach( var line in lines ) {
                buffer.Add( line );
            }
        }

        private ChatLogLine[] GetLines( Bitmap screenShot ) {
            // チャットログ欄の左にあるスクロールバーの△ボタンの位置を元に、チャットログ欄の行数を推定する
            int chatLogLines = 5;
            for( int i = 3; i <= 30; i++ ) {
                var buttonGeometry = window.GetChatLogScrollUpButtonGeometry( i );
                var buttonImage = (Bitmap)screenShot.Clone( buttonGeometry, screenShot.PixelFormat );
                if( ImageComparator.Compare( buttonImage, Resource.chat_log_scroll_up_button ) ) {
                    chatLogLines = i;
                    break;
                }
            }

            // チャットの各行について処理する
            var result = new List<ChatLogLine>();
            for( int lineIndex = 0; lineIndex < chatLogLines; lineIndex++ ) {
                var lineGeometry = window.GetChatLogLineGeometry( lineIndex, chatLogLines );
                var lineImage = (Bitmap)screenShot.Clone( lineGeometry, screenShot.PixelFormat );

                var lineType = DetectLineType( lineImage );
                var filteredLineImage = TextFinder.CreateFilteredImage( lineImage, ChatLogLine.GetLetterColorByType( lineType ) );
                var lineString = "";
                try {
                    lineString = TextFinder.Find( filteredLineImage, true, false );
                    if( lineString != "" ) {
                        result.Add( new ChatLogLine( lineString, lineType ) );
                    }
                } catch( FEZBotException e ) {
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 一行分のチャットログの画像から、その行の発言種類を判定する
        /// </summary>
        /// <param name="lineImage"></param>
        /// <returns></returns>
        private ChatLogLine.LineType DetectLineType( Bitmap lineImage ) {
            var letterColors = new Dictionary<Color, ChatLogLine.LineType>();

            foreach( object typeObject in Enum.GetValues( typeof( ChatLogLine.LineType ) ) ) {
                var type = (ChatLogLine.LineType)typeObject;
                if( type == ChatLogLine.LineType.UNKNOWN ) {
                    continue;
                }
                var color = ChatLogLine.GetLetterColorByType( type );
                letterColors.Add( color, type );
            }

            var draft = ChatLogLine.LineType.UNKNOWN;
            for( int y = 0; y < lineImage.Height; y++ ) {
                for( int x = 0; x < lineImage.Width; x++ ) {
                    var c = Color.FromArgb( 255, lineImage.GetPixel( x, y ) );
                    ChatLogLine.LineType type;
                    if( letterColors.TryGetValue( c, out type ) ) {
                        if( draft == ChatLogLine.LineType.UNKNOWN ) {
                            draft = type;
                        } else if( draft != type ) {
                            throw new FEZBotException( "複数の発言種類の文字が、同一行に存在する" );
                        }
                    }
                }
            }
            if( draft == ChatLogLine.LineType.UNKNOWN ) {
                throw new FEZBotException( "発言種類を判定できなかった" );
            } else {
                return draft;
            }
        }
    }

    public class ChatLogLine : IEquatable<ChatLogLine> {
        public enum LineType {
            /// <summary>
            /// 不明
            /// </summary>
            UNKNOWN,

            /// <summary>
            /// 範囲発言
            /// </summary>
            SAY,

            /// <summary>
            /// パーティ発言
            /// </summary>
            PARTY,

            /// <summary>
            /// 軍団発言
            /// </summary>
            ARMY,

            /// <summary>
            /// 軍団範囲発言
            /// </summary>
            ARMYSAY,

            /// <summary>
            /// 部隊発言
            /// </summary>
            FORCE,

            /// <summary>
            /// 個人発言
            /// </summary>
            TELL,

            /// <summary>
            /// 全体発言
            /// </summary>
            ALL,

            EMOTION,

            SYSTEM,
        }

        private string line;
        private LineType type;

        public ChatLogLine( string line, LineType type ) {
            this.line = line;
            this.type = type;
        }

        public string Line {
            get {
                return line;
            }
        }

        public LineType Type {
            get {
                return type;
            }
        }

        public bool Equals( ChatLogLine line ) {
            return (this.Line == line.Line && this.Type == line.Type);
        }

        /// <summary>
        /// チャットの発言種類から、デフォルトの文字色を取得する
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Color GetLetterColorByType( LineType type ) {
            switch( type ) {
                case LineType.SAY: {
                    return Color.FromArgb( 255, 255, 255, 255 );
                }
                case LineType.PARTY: {
                    return Color.FromArgb( 255, 0, 255, 255 );
                }
                case LineType.ARMY: {
                    return Color.FromArgb( 255, 255, 255, 0 );
                }
                case LineType.ARMYSAY: {
                    return Color.FromArgb( 255, 255, 187, 85 );
                }
                case LineType.FORCE: {
                    return Color.FromArgb( 255, 0, 255, 0 );
                }
                case LineType.TELL: {
                    return Color.FromArgb( 255, 255, 0, 255 );
                }
                case LineType.ALL: {
                    return Color.FromArgb( 255, 255, 0, 0 );
                }
                case LineType.EMOTION: {
                    return Color.FromArgb( 255, 102, 102, 170 );
                }
                case LineType.SYSTEM: {
                    return Color.FromArgb( 255, 221, 102, 0 );
                }
            }
            return Color.Black;
        }

        /// <summary>
        /// チャットの発言種類から、デフォルトの文字色を取得する
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetIrcColorByType( LineType type ) {
            switch( type ) {
                case LineType.SAY: {
                    return 16;
                }
                case LineType.PARTY: {
                    return 11;
                }
                case LineType.ARMY: {
                    return 8;
                }
                case LineType.ARMYSAY: {
                    return 5;
                }
                case LineType.FORCE: {
                    return 9;
                }
                case LineType.TELL: {
                    return 13;
                }
                case LineType.ALL: {
                    return 4;
                }
                case LineType.EMOTION: {
                    return 2;
                }
                case LineType.SYSTEM: {
                    return 7;
                }
            }
            return 1;
        }
    }
}
