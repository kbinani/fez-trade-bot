using System;
using System.Drawing;
using Growl.Connector;
using Growl.CoreLibrary;

class GrowlNotifyTask {
    private RuntimeSettings settings;
    private Bitmap icon;
    private string message;

    public GrowlNotifyTask( RuntimeSettings settings, string message, Bitmap icon ){
        this.settings = settings;
        this.icon = icon;
        this.message = message;
    }

    public void Run() {
        var connector = new GrowlConnector( settings.GrowlyPass, settings.GrowlyHost, settings.GrowlyPort );
        var application = new Growl.Connector.Application( "FEZ trade notify" );
        var notificationType = new NotificationType( "FEZ_TRADE_NOTIFICATION", "Trade Notification" );
        connector.Register( application, new NotificationType[] { notificationType } );
        connector.EncryptionAlgorithm = Cryptography.SymmetricAlgorithmType.PlainText;
        var callbackContext = new CallbackContext( "some fake information", "fake data" );
        var notification = new Notification(
            application.Name, notificationType.Name, DateTime.Now.Ticks.ToString(),
            "Trade Notification", message, icon, false, Priority.Normal, "0" );
            connector.Notify( notification, callbackContext );
    }
}
