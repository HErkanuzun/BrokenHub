Broken Hub
==========
Broken araçlarını tek yerden kur, güncelle, aç ve kaldır.
Install, update, open and remove the Broken tools from one place.
https://github.com/HErkanuzun/BrokenHub

TÜRKÇE
------
1. BrokenHub.exe'yi aç. Yönetici izni istemez.
2. Her aracın kartında kurulu sürüm ve GitHub'daki son sürüm görünür.
3. "Kur" ya da "Güncelle": kurulum paketi GitHub'dan indirilir, SHA-256 özeti
   sürümle gelen SHA256SUMS.txt ile karşılaştırılır, sonra kurulum çalışır.
   Aracın kendisi yönetici izni istiyorsa Windows bunu sorar.
4. "Aç" aracı başlatır; "Kaldır" aracın kendi kaldırma aracını açar.

Ayarlar: %AppData%\BrokenHub
İndirilen kurulum paketleri: %Temp%\BrokenHub (kurulumdan sonra silinir)

ENGLISH
-------
1. Open BrokenHub.exe. It doesn't need administrator rights.
2. Each tool's card shows the installed version and the latest one on GitHub.
3. "Install" or "Update" downloads the setup from GitHub, checks its SHA-256
   against the release's SHA256SUMS.txt and runs it. If the tool itself needs
   administrator rights, Windows asks.
4. "Open" starts the tool; "Uninstall" runs the tool's own uninstaller.

Settings: %AppData%\BrokenHub
Downloaded setups: %Temp%\BrokenHub (deleted after installing)

MIT License.
