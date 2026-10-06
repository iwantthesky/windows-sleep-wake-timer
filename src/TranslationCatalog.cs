using System;
using System.Collections.Generic;

internal static class TranslationCatalog
{
    internal static readonly Dictionary<string, string> Entries = new Dictionary<string, string>(StringComparer.Ordinal) {
        { "İşlem yetkileri açılamadı.", "Unable to open process privileges." },
        { "Uyku yetkisi etkinleştirilemedi. Yönetici olarak aç.", "Unable to enable the sleep privilege. Run as administrator." },
        { "Önceki uyku yetkisi geri yüklenemedi.", "Unable to restore the previous sleep privilege." },
        { "Uyku yetkisi etkin; Windows uyku çağrısı başlıyor, uyandırma olayları açık.", "Sleep privilege enabled; requesting sleep with wake events enabled." },
        { "Uyku çağrısı döndü. Sonuç=", "Sleep call returned. Result=" },
        { "Windows uyku isteğini reddetti (kod ", "Windows rejected the sleep request (code " },
        { "Bu uygulamanın bir uyandırma görevi zaten var. Önce onu iptal et; mevcut görev değiştirilmedi.", "A wake task already exists for this app. Cancel it first; the existing task was not changed." },
        { "<RegistrationInfo><Description>Uyku Zamanlayıcı: uyku sonrası tek seferlik uyandırma.</Description></RegistrationInfo>", "<RegistrationInfo><Description>Sleep and Wake Timer: one-time wake after sleep.</Description></RegistrationInfo>" },
        { "Uyandırma görevi doğrulanamadı. Bilgisayar uykuya alınmadı.", "Unable to verify the wake task. The computer was not put to sleep." },
        { "Güç denetimi zaman aşımına uğradı.", "Power check timed out." },
        { "Güç denetimi başarısız: ", "Power check failed: " },
        { "Windows'un etkin uyandırma zamanlayıcılarında bu görev bulunamadı. Uyku başlatılmayacak.", "This task was not found in the active Windows wake timers. Sleep will not start." },
        { "Uyku öncesi priz veya uyandırma ayarı değişmiş. Uyku iptal edildi.", "AC power or wake settings changed before sleep. Sleep was canceled." },
        { "Güvenilir bir gelecek uyandırma görevi yok. Uyku iptal edildi.", "There is no verified future wake task. Sleep was canceled." },
        { "Uyandırma saati çok yakın; tekrar kur.", "The wake time is too close. Schedule it again." },
        { "Windows etkin uyandırma zamanlayıcısını doğruladı. Ana saat=", "Windows verified the active wake timer. Main time=" },
        { "; yedekler +2 ve +5 dakika.", "; fallback triggers at +2 and +5 minutes." },
        { "Uyutmadan ön kontrol ", "No-sleep preflight " },
        { "Kontrol sürüyor. Uyku çağrılmayacak.", "Check in progress. Sleep will not be called." },
        { "Priz bağlantısı yok.", "AC power is not connected." },
        { "AC uyandırma zamanlayıcıları etkin değil.", "AC wake timers are not enabled." },
        { "PASS: prize takılı, uyandırma zamanlayıcıları açık.", "PASS: AC power connected and wake timers enabled." },
        { "S3 desteği doğrulanamadı.", "S3 support could not be verified." },
        { "PASS: Windows S3 uyku desteğini bildiriyor.", "PASS: Windows reports S3 sleep support." },
        { "PASS: uyku yetkisi etkinleştirildi ve eski durumuna döndürüldü.", "PASS: sleep privilege enabled and restored." },
        { "BİLGİ: Chrome uzaktan bağlantı hizmeti: ", "INFO: Chrome Remote Desktop service: " },
        { "BİLGİ: Chrome uzaktan bağlantı hizmeti kurulu değil; isteğe bağlıdır.", "INFO: Chrome Remote Desktop is not installed; it is optional." },
        { "PASS: SYSTEM görevi oluşturuldu; WakeToRun ve ana/+2/+5 dakika tetikleri doğrulandı.", "PASS: SYSTEM task created; WakeToRun and main/+2/+5 minute triggers verified." },
        { "PASS: Windows bu görevi etkin uyandırma zamanlayıcısı olarak gösteriyor.", "PASS: Windows lists this task as an active wake timer." },
        { "Bilgisayar açıkken görev saatinin gelmesi bekleniyor. Uyku çağrılmadı.", "Waiting for the scheduled task while the computer stays awake. Sleep was not called." },
        { "Planlanan görevin çalıştığı doğrulanamadı.", "The scheduled task execution could not be verified." },
        { "PASS: görev zamanında SYSTEM olarak uygulamayı çalıştırdı; uyanık tutma çağrısı kabul edildi.", "PASS: task ran the app as SYSTEM; the keep-awake request was accepted." },
        { "SONUÇ: YAZILIM ÖN KONTROLÜ GEÇTİ. Gerçek uyku, donanımın uyanması ve ağın yeniden bağlanması TEST EDİLMEDİ.", "RESULT: SOFTWARE PREFLIGHT PASSED. Actual sleep, hardware wake-up, and network reconnection were NOT TESTED." },
        { "Uyku çağrılmadı.", "Sleep was not called." },
        { "Geçici kontrol görevi silindi.", "Temporary preflight task removed." },
        { "Görev temizleme hatası: ", "Task cleanup error: " },
        { "Görev SYSTEM olarak çalışmadı.", "The task did not run as SYSTEM." },
        { "Uyanık tutma isteği reddedildi.", "The keep-awake request was rejected." },
        { "Windows uyanık tutma isteğini etkin güç isteklerinde göstermedi.", "Windows did not list the keep-awake request in active power requests." },
        { "PASS: planlanan görev çalıştı; uyku çağrılmadı. ", "PASS: scheduled task ran; sleep was not called. " },
        { "PASS: SeShutdownPrivilege etkinleştirildi ve geri yüklendi. Uyku çağrılmadı, görev oluşturulmadı. AC=", "PASS: SeShutdownPrivilege enabled and restored. No sleep called or task created. AC=" },
        { "Uyanık tutma isteği reddedildi; yedek tetikler korunuyor.", "Keep-awake request rejected; fallback triggers are retained." },
        { "Uyandırma görevi çalıştı; 10 dakika uyanık tutma isteği kabul edildi.", "Wake task ran; the 10-minute keep-awake request was accepted." },
        { " Uyandırma görevi çalıştı; bağlantı için 10 dakika uyanık tutuluyor.", " Wake task ran; keeping awake for 10 minutes for reconnection." },
        { "Uyanma kaydı yazılamadı: ", "Unable to write wake log: " },
        { "Durmuş Chrome hizmeti başlatıldı.", "Stopped Chrome Remote Desktop service started." },
        { "Chrome hizmet denetimi: ", "Chrome service check: " },
        { "Desteklenen seçenekler: --diagnose veya --preflight.", "Supported options: --diagnose or --preflight." },
        { "Uyku Zamanlayıcı", "Sleep & Wake Timer" },
        { "Uyut, belirlediğin sürede uyandır", "Sleep now, wake up later" },
        { "Kaç dakika sonra uyansın?", "Wake up after how many minutes?" },
        { "Uyku sırasında uzaktan bağlantı kesilir. Uyanma desteği bilgisayara ve güç ayarlarına bağlıdır. İlk denemeyi bilgisayarın yanındayken yap. Prize takılı kullanım gerekir.", "Remote access disconnects during sleep. Wake-up depends on your hardware and power settings. Perform the first test near the computer. AC power is required." },
        { "Uyandırmayı kur ve uyut", "Schedule wake-up and sleep" },
        { "Görevi iptal et", "Cancel task" },
        { "Hazır. Açmak hiçbir ayarı değiştirmez ve uyku başlatmaz.", "Ready. Opening the app does not change power settings or start sleep." },
        { "İptal edilemedi", "Unable to cancel" },
        { "Prize takılı olduğun doğrulanamadı. Uyku başlatılmadı.", "AC power could not be verified. Sleep was not started." },
        { "Aktif güç planında tüm uyandırma zamanlayıcıları açık değil. Güç ayarlarını otomatik değiştirmiyorum; uyku başlatılmadı.", "All wake timers are not enabled in the active power plan. Power settings were not changed; sleep was not started." },
        { "Bilgisayar uykuya alınacak ve uzaktan bağlantı kesilecek. Otomatik uyanma donanıma bağlıdır; ilk denemeyi bilgisayarın yanındayken yap. Devam edilsin mi?", "The computer will sleep and remote access will disconnect. Automatic wake-up depends on your hardware; perform the first test near the computer. Continue?" },
        { "Gerçek uyku işlemi", "Put computer to sleep" },
        { "Uyandırma görevi doğrulandı. 10 saniye sonra uyku; uyanma: ", "Wake task verified. Sleep starts in 10 seconds; wake-up: " },
        { ". İptal edebilirsin.", ". You can cancel." },
        { "Başlatılmadı: ", "Not started: " },
        { "Uyku başlatılmadı", "Sleep was not started" },
        { " saniye sonra uyku. Uyanma: ", " seconds until sleep. Wake-up: " },
        { "Uyku çağrısı tamamlandı. Yerel tanı kayıtları: %ProgramData%\\UykuZamanlayici\\Diagnostics", "Sleep call completed. Local logs: %ProgramData%\\UykuZamanlayici\\Diagnostics" },
        { "Uyku başlatılamadı: ", "Unable to start sleep: " },
        { "Windows uyku hatası", "Windows sleep error" },
        { "Uyku geri sayımı ve bu uygulamanın uyandırma görevi iptal edildi.", "Sleep countdown and this app's wake task were canceled." },
    };
    internal static string English(string text)
    {
        string translated;
        return Entries.TryGetValue(text, out translated) ? translated : text;
    }
    internal static string Turkish(string text)
    {
        foreach (KeyValuePair<string, string> entry in Entries)
            if (entry.Value == text) return entry.Key;
        return text;
    }
}
