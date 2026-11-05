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

<a id="code-signing-policy"></a>
## Code signing policy · Kod imzalama politikası

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

Ücretsiz kod imzalama hizmetini [SignPath.io](https://about.signpath.io) sağlar, sertifika [SignPath Foundation](https://signpath.org)'a aittir.

| Role · Görev | Members · Kişiler |
|---|---|
| Committers and reviewers · Geliştirenler ve inceleyenler | [HErkanuzun](https://github.com/HErkanuzun) |
| Approvers · İmzayı onaylayanlar | [HErkanuzun](https://github.com/HErkanuzun) |

Only files built by GitHub Actions from this repository are signed: the program and its setup in each release. Every signing request is approved by hand. Changes from people outside the team are reviewed before they are merged.

Sadece bu depodan GitHub Actions ile derlenen dosyalar imzalanır: her sürümdeki program ve kurulum paketi. Her imza isteği elle onaylanır. Ekip dışından gelen değişiklikler birleştirilmeden önce incelenir.

### Privacy · Gizlilik

Broken Hub connects only to GitHub (`api.github.com` and `github.com`) to look up the latest versions of the Broken tools and, when you press Install or Update, to download their setups. It sends no personal data and collects no telemetry. These requests are covered by [GitHub's privacy statement](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement). The other Broken tools do not connect to the internet at all.

Broken Hub sadece GitHub'a bağlanır: araçların son sürümlerini öğrenmek ve Kur ya da Güncelle'ye bastığında kurulum dosyalarını indirmek için. Kişisel veri göndermez, kullanım verisi toplamaz. Bu istekler için [GitHub'ın gizlilik bildirimi](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement) geçerlidir.
