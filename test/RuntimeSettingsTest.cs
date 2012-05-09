using System;
using FEZTradeBot;
using NUnit.Framework;
using System.Drawing;

namespace FEZTradeBotTest {
    class RuntimeSettingStub : RuntimeSettings {
        public new string GetFieldName( string name ) {
            return base.GetFieldName( name );
        }
    }

    class RuntimeSettingsTest {
        [TestCase]
        public static void GetFieldName() {
            var setting = new RuntimeSettingStub();
            var actual = setting.GetFieldName( "foo.barBaz.helloWorld" );
            Assert.AreEqual( "fooBarBazHelloWorld", actual );
        }
    }
}
