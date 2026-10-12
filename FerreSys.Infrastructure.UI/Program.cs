using Velopack;

namespace FerreSys.Infrastructure.UI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.Run(new Form1(args));
    }
}
