using System.Windows;

namespace Client;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        Services.Client.Instance?.SendEvent("OnDisconnect",
            Services.Client.Instance.Udp.Client.LocalEndPoint?.ToString() ?? string.Empty);
        base.OnExit(e);
    }
}