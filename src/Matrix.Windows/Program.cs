using System.Windows;

namespace Matrix.Windows;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        StudioTheme.Apply(app);
        app.Run(new OperatorWindow());
    }
}
