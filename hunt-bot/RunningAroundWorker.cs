using System;
using System.Threading;

namespace FEZTradeBot {
    /// <summary>
    /// 狩り対象のMOBに走り寄るための操作を行う
    /// </summary>
    class RunningAroundWorker {
        private FEZWindow window;

        public RunningAroundWorker( FEZWindow window ) {
            this.window = window;
        }

        public void Run() {
            LookGround();

            //TODO: なんか走り回る処理
        }

        /// <summary>
        /// 下を向く処理
        /// </summary>
        private void LookGround() {
            window.Activate();
            using( var client = new VMultiKeyboardClient() ) {
                client.ClearKey();
                client.SetKey( WindowsAPI.VK_END );
                Thread.Sleep( TimeSpan.FromSeconds( 5 ) );
                client.ClearKey();
            }
        }
    }
}
