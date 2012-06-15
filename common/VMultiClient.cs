using System;
using System.Runtime.InteropServices;

namespace FEZTradeBot {
    public class VMultiKeyboardClient : IDisposable {
        [DllImport( "vmulti.dll" )]
        private static extern IntPtr VMultiCreateKeyboardClient();

        [DllImport( "vmulti.dll" )]
        private static extern void VMultiRelease( IntPtr client );

        [DllImport( "vmulti.dll" )]
        private static extern void VMultiSetKey( IntPtr client, byte virtualKey );

        [DllImport( "vmulti.dll" )]
        private static extern void VMultiClearKey( IntPtr client );

        private IntPtr handle;

        public VMultiKeyboardClient() {
            handle = VMultiCreateKeyboardClient();
            if( handle == IntPtr.Zero ) {
                throw new CommonException( "仮想キーボードの初期化に失敗した。" );
            }
        }

        public void SetKey( byte virtualKey ) {
            VMultiSetKey( handle, virtualKey );
        }

        public void ClearKey() {
            VMultiClearKey( handle );
        }

        public void Dispose() {
            VMultiRelease( handle );
        }
    }
}
