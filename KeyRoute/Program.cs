using System.Windows.Forms;

namespace KeyRoute
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            var store = new ConfigStore(configPath);
            var mappings = store.Load();

            using var hook = new KeyboardHook();
            hook.UpdateMappings(mappings);
            hook.Start();

            Application.Run(new MainForm(hook, store, mappings));
        }
    }
}
