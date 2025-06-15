using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace VBoxWpfApp
{
    public static class ToastHelper
    {
        public static void ShowToast(string message)
        {
            var xml = new XmlDocument();
            xml.LoadXml($@"
                <toast duration='short'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>VirtualBox Manager</text>
                            <text>{message}</text>
                        </binding>
                    </visual>
                </toast>");

            var toast = new ToastNotification(xml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}