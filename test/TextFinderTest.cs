using System;
using NUnit.Framework;
using FEZTradeBot;

namespace FEZTradeBotTest {
    public class TextFinderTest {
        [TestCase]
        public static void 全角文字() {
            Assert.AreEqual( "た。", TextFinder.Find( Resource.TextFinder_全角文字 ) );
        }

        [TestCase]
        public static void 半角英数() {
            Assert.AreEqual( "status:SUCCEEDED", TextFinder.Find( Resource.TextFinder_半角英数 ) );
            Assert.AreEqual(
                "             status: SUCCEEDED",
                TextFinder.Find( Resource.TextFinder_半角英数, false, false )
            );
        }

        [TestCase]
        public static void 全角文字の左半分が空白の場合() {
            Assert.AreEqual(
                "「アベル渓谷」がネツァワル国から攻められています。",
                TextFinder.Find( Resource.TextFinder_全角文字の左半分が空白の場合, false, false )
            );
        }

        [TestCase]
        public static void 半角カナ() {
            Assert.AreEqual(
                ">>店員さん : ｱｶﾞﾊﾟｧ｡､",
                TextFinder.Find( Resource.TextFinder_半角カナ, false, false )
            );
        }
    }
}
