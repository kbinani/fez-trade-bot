using System;
using System.Drawing;
using System.Threading;

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
                    //TODO: なんか攻撃する処理
                }
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            }
        }
    }

}
