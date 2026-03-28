using System.Windows;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;

namespace DoenaSoft.FileTransferManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var io = new IOServices();
        var ui = new WindowUIServices();

        var wnd = new MainWindow(io, ui);

        wnd.Show();
    }
}
