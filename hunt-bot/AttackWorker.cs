using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FEZTradeBot {
    /// <summary>
    /// 狩り対象のMOBを攻撃するための操作を行う
    /// </summary>
    class AttackWorker {
        private bool isRunning;
        private FEZWindow window;

        public AttackWorker( FEZWindow window ) {
            this.window = window;
            this.isRunning = false;
        }

        public void Stop() {
            isRunning = false;
        }

        public void Resume() {
            isRunning = true;
        }

        public void Run() {
            while( true ) {
                if( isRunning ) {
                    var position = DetectMobPosition();
                    if( position != Point.Empty ) {
                        window.Click( position );
                    }
                }
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            }
        }

        /// <summary>
        /// 画面上の、MOBが表示されている位置を検出する。
        /// カメラの視線は、地面を見ている前提で、画面の中にMOBの体色と同じ色が一定割合で表示されていれば、
        /// そこにMOBがいると判定する
        /// </summary>
        /// <returns>MOBが表示されている位置。MOBが見つからなければPoint.Emptyを返す</returns>
        private Point DetectMobPosition() {
            var screenImage = window.CaptureWindow();
            int width = screenImage.Width;
            int height = screenImage.Height;

            // 画面をgridSize x gridSizeピクセルのグリッドに分割する
            // そのうち、画面中心から、上下に200px, 左右に300pxの範囲のみを、検査対象とする
            const int gridSize = 50;
            const int targetWidth = 600;
            const int targetHeight = 400;
            int horizontalGridCount = ((int)Math.Ceiling( (window.Width / 2.0) / gridSize )) * 2;
            int verticalGridCount = ((int)Math.Ceiling( (window.Height / 2.0) / gridSize )) * 2;

            // 全グリッドのうち、検査対象とするグリッドを取り出しておく
            int centerX = width / 2;
            int centerY = height / 2;
            List<Point> targetGrids = new List<Point>();
            for( int x = 0; x < width / gridSize + 2; ++x ) {
                for( int y = 0; y < height / gridSize + 2; ++y ) {
                    var area = GetGridBound( width, height, x, y );
                    if( (centerX - targetWidth / 2 <= area.Right && area.Left <= centerX + targetWidth / 2) &&
                        (centerY - targetHeight / 2 <= area.Bottom && area.Top <= centerY + targetHeight / 2) ) {
                            targetGrids.Add( new Point( x, y ) );
                    }
                }
            }

            // 各グリッドの全ピクセルのうち、MOBの体色のピクセルが占める割合
            double[,] percentage = new double[horizontalGridCount, verticalGridCount];
            for( int y = 0; y < verticalGridCount; y++ ) {
                for( int x = 0; x < horizontalGridCount; x++ ) {
                    percentage[x, y] = 0.0;
                }
            }

            // 各グリッドについて、MOBの体色のピクセルを調べる
            Color[,] screen = new Color[width, height];
            for( int y = 0; y < height; y++ ) {
                for( int x = 0; x < width; x++ ){
                    screen[x, y] = screenImage.GetPixel( x, y );
                }
            }

            Parallel.ForEach( targetGrids, grid => {
                var area = GetGridBound( width, height, grid.X, grid.Y );

                int total = 0;
                int match = 0;
                for( int y = area.Top; y < area.Bottom; y++ ) {
                    for( int x = area.Left; x < area.Right; x++ ) {
                        var color = screen[x, y];
                        if( color.R == 0 &&
                            3 <= color.G && color.G <= 148 &&
                            18 <= color.B && color.B <= 159 ) {
                            match++;
                        }
                        total++;
                    }
                }
                lock( percentage ) {
                    percentage[grid.X, grid.Y] = match / (double)total * 100.0;
                }
            } );

            // MOBの体色の割合が30%を超えていて、かつ最も割合の大きいグリッドの位置を、MOBの位置とみなす
            int gridX = 0;
            int gridY = 0;
            double max = 0.0;
            for( int x = 0; x < horizontalGridCount; x++ ) {
                for( int y = 0; y < verticalGridCount; y++ ) {
                    if( percentage[x, y] > 30 && max < percentage[x, y] ) {
                        max = percentage[x, y];
                        gridX = x;
                        gridY = y;
                    }
                }
            }
            if( max == 0.0 ) {
                return Point.Empty;
            } else {
                var area = GetGridBound( width, height, gridX, gridY );
                return new Point( area.Left + area.Width / 2, area.Top + area.Height / 2 );
            }
        }

        private Rectangle GetGridBound( int width, int height, int gridX, int gridY ) {
            const int gridSize = 50;
            int horizontalGridCount = ((int)Math.Ceiling( (width / 2.0) / gridSize )) * 2;
            int verticalGridCount = ((int)Math.Ceiling( (height / 2.0) / gridSize )) * 2;

            int centerX = width / 2;
            int centerY = height / 2;
            int left = centerX - ((horizontalGridCount / 2) - gridX) * gridSize;
            int top = centerY - ((verticalGridCount / 2) - gridY) * gridSize;

            return new Rectangle( left, top, gridSize, gridSize );
        }
    }
}
