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

        try
        {
            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.Run(new StartupForm(args));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "FerreSys", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
