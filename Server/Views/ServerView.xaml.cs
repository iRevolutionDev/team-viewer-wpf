using System.Windows;
using Server.ViewModels;

namespace Server;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class ServerView : Window
{
    public ServerView()
    {
        InitializeComponent();
        DataContext = new ServerViewModel();
    }
}