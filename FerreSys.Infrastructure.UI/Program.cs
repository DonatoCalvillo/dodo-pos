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
            if (args.Contains("--preview-ui"))
            {
                Directory.CreateDirectory(@"C:\Users\DDTECH 04\.gemini\antigravity\brain\a67ed11c-a077-40b7-8a05-aae284e29ed9\scratch");
                using var co = new CheckoutForm(600.00m);
                co.StartPosition = FormStartPosition.Manual;
                co.Location = new Point(100, 100);
                co.Show();
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(250);
                System.Windows.Forms.Application.DoEvents();
                using (var bmp = new Bitmap(co.Width, co.Height))
                {
                    using var g = Graphics.FromImage(bmp);
                    g.CopyFromScreen(co.Location, Point.Empty, co.Size);
                    bmp.Save(@"C:\Users\DDTECH 04\.gemini\antigravity\brain\a67ed11c-a077-40b7-8a05-aae284e29ed9\scratch\checkout_verify.png");
                }
                co.Close();

                var db = new FerreSys.Infrastructure.Persistence.SqliteDbContext();
                var userRepo = new FerreSys.Infrastructure.Persistence.SqliteUserRepository(db);
                new FerreSys.Application.EnsureAdminUserUseCase(userRepo).Execute();
                var user = userRepo.GetByUsername("admin")!;
                var subSvc = new LocalSubscriptionService();
                using var f1 = new Form1(["--no-seed"], subSvc, user);
                typeof(Form1).GetField("_openingCashPromptShown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(f1, true);
                f1.Show();
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(350);
                System.Windows.Forms.Application.DoEvents();
                using (var bmp1 = new Bitmap(f1.Width, f1.Height))
                {
                    using var g1 = Graphics.FromImage(bmp1);
                    g1.CopyFromScreen(f1.Location, Point.Empty, f1.Size);
                    bmp1.Save(@"C:\Users\DDTECH 04\.gemini\antigravity\brain\a67ed11c-a077-40b7-8a05-aae284e29ed9\scratch\form1_verify.png");
                }
                f1.Close();
                return;
            }
            System.Windows.Forms.Application.Run(new StartupForm(args));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Posly", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
