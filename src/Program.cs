using System;
using System.Drawing;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    internal const string TaskName = "UykuZamanlayici-Uyandir";
    internal const string ProbePrefix = "UykuZamanlayici-OnKontrol-";
    internal static string DataPath(string file)
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UykuZamanlayici", "Diagnostics");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, file);
    }
    [DllImport("kernel32.dll")] internal static extern uint SetThreadExecutionState(uint flags);
    [DllImport("powrprof.dll")] static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);
    [DllImport("powrprof.dll")] static extern uint PowerReadACValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup, ref Guid setting, out uint value);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr memory);
    [DllImport("powrprof.dll", SetLastError=true)]
    [return: MarshalAs(UnmanagedType.U1)]
    static extern bool SetSuspendState([MarshalAs(UnmanagedType.U1)] bool hibernate, [MarshalAs(UnmanagedType.U1)] bool force, [MarshalAs(UnmanagedType.U1)] bool disableWake);
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal struct TokenPrivilege { public uint Count; public long Luid; public uint Attributes; }
    [DllImport("advapi32.dll", SetLastError=true)] static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool LookupPrivilegeValue(string system, string name, out long luid);
    [DllImport("advapi32.dll", SetLastError=true)] static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TokenPrivilege state, int length, out TokenPrivilege previous, out int required);
    [DllImport("advapi32.dll", EntryPoint="AdjustTokenPrivileges", SetLastError=true)] static extern bool RestoreTokenPrivileges(IntPtr token, bool disableAll, ref TokenPrivilege state, int length, IntPtr previous, IntPtr required);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] static extern void SetLastError(uint error);

    internal sealed class ShutdownPrivilege : IDisposable
    {
        IntPtr token;
        TokenPrivilege previous;
        bool changed;
        internal ShutdownPrivilege()
        {
            if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, 0x28, out token))
                throw new Win32Exception(Marshal.GetLastWin32Error(), Loc.T("İşlem yetkileri açılamadı."));
            try {
                long luid;
                if (!LookupPrivilegeValue(null, "SeShutdownPrivilege", out luid)) throw new Win32Exception(Marshal.GetLastWin32Error());
                TokenPrivilege desired = new TokenPrivilege { Count=1, Luid=luid, Attributes=2 };
                int needed;
                SetLastError(0);
                bool ok = AdjustTokenPrivileges(token, false, ref desired, Marshal.SizeOf(typeof(TokenPrivilege)), out previous, out needed);
                int error = Marshal.GetLastWin32Error();
                if (!ok || error != 0) throw new Win32Exception(error, Loc.T("Uyku yetkisi etkinleştirilemedi. Yönetici olarak aç."));
                changed = previous.Count > 0;
            } catch { CloseHandle(token); token=IntPtr.Zero; throw; }
        }
        public void Dispose()
        {
            if (token == IntPtr.Zero) return;
            try {
                if (changed) {
                    SetLastError(0);
                    bool ok = RestoreTokenPrivileges(token, false, ref previous, 0, IntPtr.Zero, IntPtr.Zero);
                    int error = Marshal.GetLastWin32Error();
                    if (!ok || error != 0) throw new Win32Exception(error, Loc.T("Önceki uyku yetkisi geri yüklenemedi."));
                }
            } finally { CloseHandle(token); token=IntPtr.Zero; }
        }
    }

    internal static void Log(string text)
    {
        try { File.AppendAllText(DataPath("uyku-kaydi.txt"), DateTime.Now.ToString("o") + " " + text + Environment.NewLine); } catch { }
    }

    internal static void Sleep()
    {
        using (ShutdownPrivilege privilege = new ShutdownPrivilege()) {
            Log(Loc.T("Uyku yetkisi etkin; Windows uyku çağrısı başlıyor, uyandırma olayları açık."));
            bool result = SetSuspendState(false, false, false);
            int error = Marshal.GetLastWin32Error();
            Log(Loc.T("Uyku çağrısı döndü. Sonuç=") + result + "; Win32=" + error);
            if (!result) throw new Win32Exception(error, Loc.T("Windows uyku isteğini reddetti (kod ") + error + ").");
        }
    }

    internal static dynamic TaskFolder()
    {
        dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
        service.Connect();
        return service.GetFolder("\\");
    }

    internal static bool WakeTimersAllowed()
    {
        IntPtr ptr;
        if (PowerGetActiveScheme(IntPtr.Zero, out ptr) != 0) return false;
        try {
            Guid scheme = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
            Guid sleep = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
            Guid wake = new Guid("bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d");
            uint value;
            return PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref sleep, ref wake, out value) == 0 && value == 1;
        } finally { LocalFree(ptr); }
    }

    internal static void CancelWake()
    {
        DeleteTask(TaskName);
    }
    static void DeleteTask(string name)
    {
        try { TaskFolder().DeleteTask(name, 0); }
        catch (FileNotFoundException) { }
        catch (COMException ex) { if ((uint)ex.ErrorCode != 0x80070002) throw; }
    }

    internal static void ArmWake(DateTime when)
    {
        ArmTask(when, TaskName, false);
    }
    static string TimeTrigger(DateTime when)
    {
        return "<TimeTrigger><StartBoundary>" + when.ToString("yyyy-MM-ddTHH:mm:ss") + "</StartBoundary><Enabled>true</Enabled></TimeTrigger>";
    }
    static void ArmTask(DateTime when, string name, bool probe)
    {
        dynamic folder = TaskFolder();
        try {
            dynamic existing = folder.GetTask(name);
            if (existing != null) throw new InvalidOperationException(Loc.T("Bu uygulamanın bir uyandırma görevi zaten var. Önce onu iptal et; mevcut görev değiştirilmedi."));
        } catch (FileNotFoundException) { }
        catch (COMException ex) { if ((uint)ex.ErrorCode != 0x80070002) throw; }
        string exe = SecurityElement.Escape(Application.ExecutablePath);
        string triggers = TimeTrigger(when) + TimeTrigger(when.AddMinutes(2)) + TimeTrigger(when.AddMinutes(5));
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>" +
            "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">" +
            Loc.T("<RegistrationInfo><Description>Uyku Zamanlayıcı: uyku sonrası tek seferlik uyandırma.</Description></RegistrationInfo>") +
            "<Triggers>" + triggers + "</Triggers>" +
            "<Principals><Principal id=\"System\"><UserId>S-1-5-18</UserId><RunLevel>HighestAvailable</RunLevel></Principal></Principals>" +
            "<Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>" +
            "<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><StartWhenAvailable>true</StartWhenAvailable><Enabled>true</Enabled>" +
            "<WakeToRun>true</WakeToRun><ExecutionTimeLimit>PT12M</ExecutionTimeLimit></Settings>" +
            "<Actions Context=\"System\"><Exec><Command>" + exe + "</Command><Arguments>" + (probe ? "--probe " + SecurityElement.Escape(name) : "--wake") + " --language " + Loc.Code + "</Arguments></Exec></Actions></Task>";
        dynamic task = folder.RegisterTask(name, xml, 2, "SYSTEM", null, 5, null);
        if (!task.Enabled || !task.Definition.Settings.WakeToRun || (int)task.Definition.Triggers.Count != 3 || ((DateTime)task.NextRunTime) <= DateTime.Now) {
            DeleteTask(name);
            throw new InvalidOperationException(Loc.T("Uyandırma görevi doğrulanamadı. Bilgisayar uykuya alınmadı."));
        }
    }

    static string PowerCfg(string argument)
    {
        ProcessStartInfo info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe"), argument);
        info.UseShellExecute=false; info.CreateNoWindow=true; info.RedirectStandardOutput=true; info.RedirectStandardError=true;
        using (Process process = Process.Start(info)) {
            string text=process.StandardOutput.ReadToEnd(); string error=process.StandardError.ReadToEnd();
            if (!process.WaitForExit(10000)) throw new InvalidOperationException(Loc.T("Güç denetimi zaman aşımına uğradı."));
            if (process.ExitCode != 0) throw new InvalidOperationException(Loc.T("Güç denetimi başarısız: ") + error + text);
            return text;
        }
    }
    static string VerifyWakeTimer(string name)
    {
        string text="";
        for (int i=0;i<6;i++) {
            text=PowerCfg("/waketimers");
            if (text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return text;
            Thread.Sleep(500);
        }
        throw new InvalidOperationException(Loc.T("Windows'un etkin uyandırma zamanlayıcılarında bu görev bulunamadı. Uyku başlatılmayacak."));
    }
    internal static void VerifyBeforeSleep(DateTime when)
    {
        if (SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online || !WakeTimersAllowed())
            throw new InvalidOperationException(Loc.T("Uyku öncesi priz veya uyandırma ayarı değişmiş. Uyku iptal edildi."));
        dynamic task=TaskFolder().GetTask(TaskName);
        if (!task.Enabled || !task.Definition.Settings.WakeToRun || ((DateTime)task.NextRunTime) < DateTime.Now.AddSeconds(10))
            throw new InvalidOperationException(Loc.T("Güvenilir bir gelecek uyandırma görevi yok. Uyku iptal edildi."));
        VerifyWakeTimer(TaskName);
        if (when < DateTime.Now.AddSeconds(10)) throw new InvalidOperationException(Loc.T("Uyandırma saati çok yakın; tekrar kur."));
        Log(Loc.T("Windows etkin uyandırma zamanlayıcısını doğruladı. Ana saat=") + when.ToString("o") + Loc.T("; yedekler +2 ve +5 dakika."));
    }
    static void Preflight()
    {
        string name=ProbePrefix + Guid.NewGuid().ToString("N");
        string receipt=DataPath(name+".txt");
        string reportPath=DataPath("on-kontrol.txt");
        string report=Loc.T("Uyutmadan ön kontrol ") + DateTime.Now.ToString("o") + Environment.NewLine;
        File.WriteAllText(reportPath,report+Loc.T("Kontrol sürüyor. Uyku çağrılmayacak."));
        try {
            if (SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online) throw new InvalidOperationException(Loc.T("Priz bağlantısı yok."));
            if (!WakeTimersAllowed()) throw new InvalidOperationException(Loc.T("AC uyandırma zamanlayıcıları etkin değil."));
            report+=Loc.T("PASS: prize takılı, uyandırma zamanlayıcıları açık.")+Environment.NewLine;
            if (PowerCfg("/a").IndexOf("S3",StringComparison.OrdinalIgnoreCase)<0) throw new InvalidOperationException(Loc.T("S3 desteği doğrulanamadı."));
            report+=Loc.T("PASS: Windows S3 uyku desteğini bildiriyor.")+Environment.NewLine;
            using (ShutdownPrivilege privilege=new ShutdownPrivilege()) { }
            report+=Loc.T("PASS: uyku yetkisi etkinleştirildi ve eski durumuna döndürüldü.")+Environment.NewLine;
            try {
                using (ServiceController chrome=new ServiceController("chromoting")) {
                    report+=Loc.T("BİLGİ: Chrome uzaktan bağlantı hizmeti: ")+chrome.Status+Environment.NewLine;
                }
            } catch (InvalidOperationException) { report+=Loc.T("BİLGİ: Chrome uzaktan bağlantı hizmeti kurulu değil; isteğe bağlıdır.")+Environment.NewLine; }
            DateTime due=DateTime.Now.AddSeconds(25);
            ArmTask(due,name,true);
            report+=Loc.T("PASS: SYSTEM görevi oluşturuldu; WakeToRun ve ana/+2/+5 dakika tetikleri doğrulandı.")+Environment.NewLine;
            string timers=VerifyWakeTimer(name);
            File.WriteAllText(DataPath("on-kontrol-zamanlayici.txt"),timers);
            report+=Loc.T("PASS: Windows bu görevi etkin uyandırma zamanlayıcısı olarak gösteriyor.")+Environment.NewLine;
            File.WriteAllText(reportPath,report+Loc.T("Bilgisayar açıkken görev saatinin gelmesi bekleniyor. Uyku çağrılmadı."));
            DateTime deadline=due.AddSeconds(35);
            while (DateTime.Now<deadline && !File.Exists(receipt)) Thread.Sleep(500);
            if (!File.Exists(receipt)) throw new InvalidOperationException(Loc.T("Planlanan görevin çalıştığı doğrulanamadı."));
            string content=File.ReadAllText(receipt);
            if (!content.StartsWith("PASS:")) throw new InvalidOperationException(content);
            report+=Loc.T("PASS: görev zamanında SYSTEM olarak uygulamayı çalıştırdı; uyanık tutma çağrısı kabul edildi.")+Environment.NewLine;
            report+=Loc.T("SONUÇ: YAZILIM ÖN KONTROLÜ GEÇTİ. Gerçek uyku, donanımın uyanması ve ağın yeniden bağlanması TEST EDİLMEDİ.");
        } catch (Exception ex) { report+="FAIL: "+ex.Message+Environment.NewLine+Loc.T("Uyku çağrılmadı."); }
        finally {
            try { DeleteTask(name); report+=Environment.NewLine+Loc.T("Geçici kontrol görevi silindi."); }
            catch (Exception ex) { report+=Environment.NewLine+Loc.T("Görev temizleme hatası: ")+ex.Message; }
            File.WriteAllText(reportPath,report);
        }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        Loc.Load();
        if (args.Length >= 2 && args[args.Length - 2] == "--language") {
            string language = args[args.Length - 1];
            if (language != "en" && language != "tr") { Environment.ExitCode = 2; return; }
            Loc.English = language == "en";
            Array.Resize(ref args, args.Length - 2);
        }
        if (args.Length==1 && args[0]=="--preflight") { Preflight(); return; }
        Guid probeId;
        if (args.Length==2 && args[0]=="--probe" && args[1].StartsWith(ProbePrefix, StringComparison.Ordinal) && Guid.TryParseExact(args[1].Substring(ProbePrefix.Length), "N", out probeId)) {
            string report;
            try {
                if (!System.Security.Principal.WindowsIdentity.GetCurrent().IsSystem) throw new InvalidOperationException(Loc.T("Görev SYSTEM olarak çalışmadı."));
                if (SetThreadExecutionState(0x80000001)==0) throw new InvalidOperationException(Loc.T("Uyanık tutma isteği reddedildi."));
                string requests=PowerCfg("/requests");
                if (requests.IndexOf(Path.GetFileName(Application.ExecutablePath),StringComparison.OrdinalIgnoreCase)<0)
                    throw new InvalidOperationException(Loc.T("Windows uyanık tutma isteğini etkin güç isteklerinde göstermedi."));
                File.WriteAllText(DataPath("on-kontrol-guc-istegi.txt"),requests);
                report=Loc.T("PASS: planlanan görev çalıştı; uyku çağrılmadı. ")+DateTime.Now.ToString("o");
            } catch (Exception ex) {report="FAIL: "+ex.Message;}
            finally {SetThreadExecutionState(0x80000000);}
            File.WriteAllText(DataPath(args[1]+".txt"),report);
            return;
        }
        if (args.Length == 1 && args[0] == "--diagnose") {
            string report;
            try {
                using (ShutdownPrivilege privilege = new ShutdownPrivilege()) {
                    report = Loc.T("PASS: SeShutdownPrivilege etkinleştirildi ve geri yüklendi. Uyku çağrılmadı, görev oluşturulmadı. AC=") +
                        SystemInformation.PowerStatus.PowerLineStatus + "; WakeTimers=" + WakeTimersAllowed();
                }
            } catch (Exception ex) { report = "FAIL: " + ex.Message; }
            File.WriteAllText(DataPath("yetki-kontrolu.txt"), report);
            return;
        }
        if (args.Length == 1 && args[0] == "--wake") {
            try {
                // This runs only when the scheduled task fires. Keep the machine awake
                // long enough for the network and the remote desktop service to recover.
                if (SetThreadExecutionState(0x80000001)==0) throw new InvalidOperationException(Loc.T("Uyanık tutma isteği reddedildi; yedek tetikler korunuyor."));
                Log(Loc.T("Uyandırma görevi çalıştı; 10 dakika uyanık tutma isteği kabul edildi."));
                try { File.AppendAllText(DataPath("uyanma-kaydi.txt"),
                    DateTime.Now.ToString("o") + Loc.T(" Uyandırma görevi çalıştı; bağlantı için 10 dakika uyanık tutuluyor.") + Environment.NewLine);
                } catch (Exception ex) { Log(Loc.T("Uyanma kaydı yazılamadı: ")+ex.Message); }
                try {
                    using (ServiceController chrome=new ServiceController("chromoting")) {
                        if (chrome.Status==ServiceControllerStatus.Stopped) { chrome.Start(); chrome.WaitForStatus(ServiceControllerStatus.Running,TimeSpan.FromSeconds(30)); Log(Loc.T("Durmuş Chrome hizmeti başlatıldı.")); }
                    }
                } catch (Exception ex) {Log(Loc.T("Chrome hizmet denetimi: ")+ex.Message);}
                Thread.Sleep(TimeSpan.FromMinutes(10));
                CancelWake();
            } catch (Exception ex) {
                try { File.AppendAllText(DataPath("uyanma-kaydi.txt"), ex.Message + Environment.NewLine); } catch { }
            } finally { SetThreadExecutionState(0x80000000); }
            return;
        }
        if (args.Length != 0) {
            MessageBox.Show(Loc.T("Desteklenen seçenekler: --diagnose veya --preflight."), Loc.T("Uyku Zamanlayıcı"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (Loc.ReadCode(Loc.SettingsPath) == null) {
            try { if (!Loc.SelectLanguage(null)) return; }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Language / Dil", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        }
        Application.Run(new SleepWindow());
    }
}

internal sealed class SleepWindow : Form
{
    readonly NumericUpDown minutes = new NumericUpDown();
    readonly Button start = new Button();
    readonly Button cancel = new Button();
    readonly Label status = new Label();
    readonly Button languageButton = new Button();
    readonly System.Windows.Forms.Timer countdown = new System.Windows.Forms.Timer();
    int seconds;
    bool pending;
    DateTime wakeAt;

    public SleepWindow()
    {
        Text = Loc.Choose("Uyku Zamanlayıcı 1.1.0", "Sleep & Wake Timer 1.1.0");
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(560, 405);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 247, 250);
        Controls.Add(new Label { Text = Loc.T("Uyut, belirlediğin sürede uyandır"), Location = new Point(24, 22), Size = new Size(510, 34), Font = new Font("Segoe UI", 16, FontStyle.Bold) });
        Controls.Add(new Label { Text = Loc.T("Kaç dakika sonra uyansın?"), Location = new Point(24, 80), Size = new Size(310, 25) });
        minutes.SetBounds(354, 76, 170, 30); minutes.Minimum = 1; minutes.Maximum = 1440; minutes.Value = 15; Controls.Add(minutes);
        Controls.Add(new Label {
            Text = Loc.T("Uyku sırasında uzaktan bağlantı kesilir. Uyanma desteği bilgisayara ve güç ayarlarına bağlıdır. İlk denemeyi bilgisayarın yanındayken yap. Prize takılı kullanım gerekir."),
            Location = new Point(24, 125), Size = new Size(510, 84), ForeColor = Color.FromArgb(125, 65, 20)
        });
        start.Text = Loc.T("Uyandırmayı kur ve uyut"); start.SetBounds(24, 224, 310, 40); start.Click += StartSleep; Controls.Add(start);
        cancel.Text = Loc.T("Görevi iptal et"); cancel.SetBounds(350, 224, 184, 40); cancel.Click += CancelSleep; Controls.Add(cancel);
        status.Text = Loc.T("Hazır. Açmak hiçbir ayarı değiştirmez ve uyku başlatmaz."); status.SetBounds(24, 284, 510, 65); Controls.Add(status);
        languageButton.Text = "English / Türkçe";
        languageButton.SetBounds(350, 357, 184, 32);
        languageButton.Click += ChangeLanguage; Controls.Add(languageButton);
        countdown.Interval = 1000; countdown.Tick += Tick;
        FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (!pending) return;
            try { CancelPending(); } catch (Exception ex) { e.Cancel = true; MessageBox.Show(ex.Message, Loc.T("İptal edilemedi")); }
        };
    }

    void ChangeLanguage(object sender, EventArgs e)
    {
        if (pending) return;
        try {
            if (!Loc.SelectLanguage(this)) return;
            foreach (Control control in Controls) {
                if (control is Label || control is Button)
                    control.Text = Loc.T(TranslationCatalog.Turkish(control.Text));
            }
            Text = Loc.Choose("Uyku Zamanlayıcı 1.1.0", "Sleep & Wake Timer 1.1.0");
            status.Text = Loc.T("Hazır. Açmak hiçbir ayarı değiştirmez ve uyku başlatmaz.");
        } catch (Exception ex) { MessageBox.Show(ex.Message, "Language / Dil", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    void StartSleep(object sender, EventArgs e)
    {
        try {
            if (SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online)
                throw new InvalidOperationException(Loc.T("Prize takılı olduğun doğrulanamadı. Uyku başlatılmadı."));
            if (!Program.WakeTimersAllowed())
                throw new InvalidOperationException(Loc.T("Aktif güç planında tüm uyandırma zamanlayıcıları açık değil. Güç ayarlarını otomatik değiştirmiyorum; uyku başlatılmadı."));
            if (MessageBox.Show(Loc.T("Bilgisayar uykuya alınacak ve uzaktan bağlantı kesilecek. Otomatik uyanma donanıma bağlıdır; ilk denemeyi bilgisayarın yanındayken yap. Devam edilsin mi?"), Loc.T("Gerçek uyku işlemi"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            seconds = 10;
            wakeAt = DateTime.Now.AddMinutes((double)minutes.Value).AddSeconds(seconds);
            Program.ArmWake(wakeAt);
            pending = true; start.Enabled = false; minutes.Enabled = false; languageButton.Enabled = false;
            status.Text = Loc.T("Uyandırma görevi doğrulandı. 10 saniye sonra uyku; uyanma: ") + wakeAt.ToString("HH:mm:ss") + Loc.T(". İptal edebilirsin.");
            countdown.Start();
        } catch (Exception ex) {
            status.Text = Loc.T("Başlatılmadı: ") + ex.Message;
            Program.Log(status.Text);
            MessageBox.Show(status.Text, Loc.T("Uyku başlatılmadı"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void Tick(object sender, EventArgs e)
    {
        seconds--;
        if (seconds > 0) { status.Text = seconds + Loc.T(" saniye sonra uyku. Uyanma: ") + wakeAt.ToString("HH:mm:ss") + "."; return; }
        countdown.Stop(); pending = false;
        try {
            Program.VerifyBeforeSleep(wakeAt);
            Program.Sleep();
            status.Text = Loc.T("Uyku çağrısı tamamlandı. Yerel tanı kayıtları: %ProgramData%\\UykuZamanlayici\\Diagnostics");
        } catch (Exception ex) {
            try { Program.CancelWake(); } catch (Exception cleanup) { Program.Log(Loc.T("Görev temizleme hatası: ") + cleanup.Message); }
            status.Text = Loc.T("Uyku başlatılamadı: ") + ex.Message;
            Program.Log(status.Text);
            MessageBox.Show(status.Text, Loc.T("Windows uyku hatası"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        } finally { start.Enabled = true; minutes.Enabled = true; languageButton.Enabled = true; }
    }

    void CancelPending()
    {
        countdown.Stop();
        Program.CancelWake();
        pending = false; start.Enabled = true; minutes.Enabled = true; languageButton.Enabled = true;
        status.Text = Loc.T("Uyku geri sayımı ve bu uygulamanın uyandırma görevi iptal edildi.");
    }
    void CancelSleep(object sender, EventArgs e) { try { CancelPending(); } catch (Exception ex) { status.Text = ex.Message; } }
}
