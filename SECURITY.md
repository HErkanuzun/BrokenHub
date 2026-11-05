# Security Policy · Güvenlik Politikası

## Supported versions · Desteklenen sürümler

Only the [latest release](https://github.com/HErkanuzun/BrokenHub/releases/latest) receives fixes.
Düzeltmeler yalnızca [son sürüme](https://github.com/HErkanuzun/BrokenHub/releases/latest) gelir.

## Reporting a vulnerability · Güvenlik açığı bildirme

Please **do not** open a public issue for security problems. Report them privately on GitHub:
**Security → Report a vulnerability** ([link](https://github.com/HErkanuzun/BrokenHub/security/advisories/new)).

Güvenlik sorunları için lütfen herkese açık bir konu (issue) **açma**. GitHub'da gizli olarak bildir:
**Security → Report a vulnerability** ([bağlantı](https://github.com/HErkanuzun/BrokenHub/security/advisories/new)).

## What the app does on your computer · Program bilgisayarında ne yapar

| | |
|---|---|
| Administrator rights · Yönetici izni | Never for Broken Hub itself. Setups of tools that need it (Disk Resizer, Space Finder) trigger Windows' UAC prompt. · Broken Hub kendisi hiç istemez. İzin isteyen araçların kurulumunda Windows UAC sorar. |
| Network · Ağ | GitHub API (`api.github.com/repos/HErkanuzun/<tool>/releases`) when opened and on Refresh; release downloads from `github.com` when you press Install/Update. No telemetry. · Açılışta ve Yenile'de GitHub API'si; Kur/Güncelle'ye basınca GitHub'dan indirme. Veri toplamaz. |
| Downloads · İndirmeler | Saved to `%Temp%\BrokenHub`, checked against the release's `SHA256SUMS.txt` (mismatch → deleted, nothing installed), run with `/SILENT`, deleted afterwards. · `%Temp%\BrokenHub`'a iner, SHA-256 ile doğrulanır, sessiz kurulur, sonra silinir. |
| Uninstalling tools · Araç kaldırma | Runs the tool's registered uninstaller. · Aracın kayıtlı kaldırma aracını çalıştırır. |
| Reading · Okuma | Each tool's uninstall entry in the registry (read-only) and whether it is running. · Her aracın kaldırma kaydı (salt okunur) ve açık olup olmadığı. |
| Files · Dosyalar | `%AppData%\BrokenHub\settings.txt` (language, window position, preview toggle). · Dil, pencere konumu, önizleme seçimi. |
| Registry · Kayıt defteri | Writes nothing except the installer's own uninstall entry. · Kurulum paketinin kaldırma kaydı dışında hiçbir şey yazmaz. |

Uninstalling from Windows "Apps" removes the program and asks whether to delete `%AppData%\BrokenHub`.
Windows "Uygulamalar"dan kaldırınca program silinir ve ayar klasörünün de silinip silinmeyeceği sorulur.

## Verifying releases · Sürümleri doğrulama

Release files are built from this repository by GitHub Actions. Each release includes `SHA256SUMS.txt`:

```powershell
Get-FileHash .\BrokenHub-1.0-Setup.exe -Algorithm SHA256
```
