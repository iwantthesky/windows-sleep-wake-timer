using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class InstallerProgram
{
    internal static byte[] Payload()
    {
        using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApplicationPayload")) {
            if (input == null) throw new InvalidOperationException("Application payload is missing.");
            using (MemoryStream output = new MemoryStream()) {
                input.CopyTo(output);
                byte[] bytes = output.ToArray();
                if (bytes.Length < 4096 || bytes[0] != 'M' || bytes[1] != 'Z')
                    throw new InvalidOperationException("Invalid application payload.");
                return bytes;
            }
        }
    }
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--verify") {
            try { Payload(); Environment.ExitCode = 0; }
            catch { Environment.ExitCode = 1; }
            return;
        }
        if (args.Length != 0) { Environment.ExitCode = 2; return; }
        Loc.Load();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerWindow());
    }
}

internal sealed class InstallerWindow : Form
{
    readonly ComboBox language = new ComboBox();
    readonly Label heading = new Label();
    readonly Label instructions = new Label();
    readonly Label languageLabel = new Label();
    readonly Label result = new Label();
    readonly Button install = new Button();
    readonly Button close = new Button();
    readonly CheckBox desktop = new CheckBox();
    bool installed;
    internal InstallerWindow()
    {
        ClientSize = new Size(560, 380);
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        heading.SetBounds(24, 20, 512, 35); heading.Font = new Font(Font.FontFamily, 16, FontStyle.Bold); Controls.Add(heading);
        languageLabel.SetBounds(24, 73, 210, 30); Controls.Add(languageLabel);
        language.SetBounds(245, 70, 290, 30); language.DropDownStyle = ComboBoxStyle.DropDownList;
        language.Items.AddRange(new object[] { "English", "Türkçe" });
        language.SelectedIndex = Loc.English ? 0 : 1;
        language.SelectedIndexChanged += delegate { Loc.English = language.SelectedIndex == 0; UpdateText(); };
        Controls.Add(language);
        instructions.SetBounds(24, 120, 512, 80); Controls.Add(instructions);
        desktop.SetBounds(24, 208, 510, 30); desktop.Checked = true; Controls.Add(desktop);
        install.SetBounds(24, 255, 250, 42); install.Click += Install; Controls.Add(install);
        close.SetBounds(290, 255, 246, 42); close.Click += delegate { Close(); }; Controls.Add(close);
        result.SetBounds(24, 310, 512, 60); Controls.Add(result);
        UpdateText();
    }
    void UpdateText()
    {
        Text = Loc.Choose("Uyku Zamanlayıcı Kurulumu 1.1.0", "Sleep & Wake Timer Setup 1.1.0");
        heading.Text = Loc.Choose("Uyku Zamanlayıcı", "Sleep & Wake Timer");
        languageLabel.Text = "Language / Dil";
        instructions.Text = Loc.Choose(
            "Uygulama yalnız bu Windows kullanıcısı için kurulacak. Seçilen dil uygulamada kullanılacak. Kurulum uyku başlatmaz veya güç ayarlarını değiştirmez.",
            "Install for this Windows user only. The app will use the selected language. Setup does not start sleep or change power settings.");
        desktop.Text = Loc.Choose("Masaüstü kısayolu oluştur", "Create a desktop shortcut");
        install.Text = Loc.Choose("Kur", "Install");
        close.Text = installed ? Loc.Choose("Kapat", "Close") : Loc.Choose("İptal", "Cancel");
        result.Text = installed ? Loc.Choose("Kurulum tamamlandı. Uygulamayı kısayoldan açabilirsin.", "Installed. Open the app using its shortcut.") :
            Loc.Choose("Uygulama açılırken Windows yönetici izni ister.", "Windows administrator permission is required when opening the app.");
    }
    static void CheckExistingWakeTask()
    {
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
        service.Connect();
        dynamic folder = service.GetFolder("\\");
        try {
            dynamic task = folder.GetTask("UykuZamanlayici-Uyandir");
            if (task != null) throw new InvalidOperationException(Loc.Choose(
                "Bir uyandırma görevi etkin. Önce uygulamadan görevi iptal et veya tamamlanmasını bekle.",
                "A wake task exists. Cancel it in the app or wait for it to finish before installing."));
        } catch (FileNotFoundException) { }
        catch (COMException ex) { if ((uint)ex.ErrorCode != 0x80070002) throw; }
    }
    static void CreateShortcut(string path, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        dynamic shortcut = shell.CreateShortcut(path);
        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = Path.GetDirectoryName(target);
        shortcut.IconLocation = target + ",0";
        shortcut.Description = "Windows Sleep & Wake Timer";
        shortcut.Save();
    }
    void Install(object sender, EventArgs e)
    {
        string temporary = null;
        try {
            CheckExistingWakeTask();
            byte[] payload = InstallerProgram.Payload();
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WindowsSleepWakeTimer");
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, "UykuZamanlayici.exe");
            temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temporary, payload);
            if (File.Exists(target)) {
                string backup = target + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
                File.Replace(temporary, target, backup);
            } else File.Move(temporary, target);
            temporary = null;
            Loc.SaveCode(Loc.SettingsPath, language.SelectedIndex == 0 ? "en" : "tr");
            CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Windows Sleep Wake Timer.lnk"), target);
            if (desktop.Checked)
                CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Windows Sleep Wake Timer.lnk"), target);
            installed = true; install.Enabled = false; language.Enabled = false; desktop.Enabled = false;
            UpdateText();
        } catch (Exception ex) {
            result.Text = Loc.Choose("Kurulum tamamlanamadı: ", "Setup could not finish: ") + ex.Message;
            MessageBox.Show(result.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        } finally {
            if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
