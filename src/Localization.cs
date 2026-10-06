using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

internal static class Loc
{
    internal static bool English = true;
    internal static string Code { get { return English ? "en" : "tr"; } }
    internal static string SettingsPath {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsSleepWakeTimer", "language.txt"); }
    }
    internal static string ReadCode(string path)
    {
        try { string code = File.ReadAllText(path).Trim(); return code == "en" || code == "tr" ? code : null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }
    internal static void SaveCode(string path, string code)
    {
        if (code != "en" && code != "tr") throw new ArgumentException("Unsupported language.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllText(temporary, code);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static void Load()
    {
        string code = ReadCode(SettingsPath);
        English = code == null ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "tr" : code == "en";
    }
    internal static string Choose(string tr, string en) { return English ? en : tr; }
    internal static bool SelectLanguage(IWin32Window owner)
    {
        using (LanguageWindow window = new LanguageWindow(Code)) {
            if (window.ShowDialog(owner) != DialogResult.OK) return false;
            SaveCode(SettingsPath, window.LanguageCode);
            English = window.LanguageCode == "en";
            return true;
        }
    }
    internal static string T(string text) { return English ? TranslationCatalog.English(text) : text; }
}

internal sealed class LanguageWindow : Form
{
    readonly ComboBox languages = new ComboBox();
    internal string LanguageCode { get { return languages.SelectedIndex == 1 ? "tr" : "en"; } }
    internal LanguageWindow(string code)
    {
        Text = "Language / Dil";
        ClientSize = new Size(410, 195);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "Choose your language / Dilini seç", Location = new Point(24, 24), Size = new Size(365, 30) });
        languages.DropDownStyle = ComboBoxStyle.DropDownList;
        languages.Items.AddRange(new object[] { "English", "Türkçe" });
        languages.SelectedIndex = code == "tr" ? 1 : 0;
        languages.SetBounds(24, 65, 360, 30); Controls.Add(languages);
        Button continueButton = new Button { Text = "Continue / Devam", DialogResult = DialogResult.OK };
        continueButton.SetBounds(204, 125, 180, 40); Controls.Add(continueButton);
        Button cancelButton = new Button { Text = "Cancel / İptal", DialogResult = DialogResult.Cancel };
        cancelButton.SetBounds(24, 125, 164, 40); Controls.Add(cancelButton);
        AcceptButton = continueButton; CancelButton = cancelButton;
    }
}
