using System;
using System.IO;

namespace BrokenHub
{
    // Basit ayar dosyası: %AppData%\BrokenHub\settings.txt (anahtar=değer)
    class Settings
    {
        public string Lang = "";
        public bool Previews = false;   // önizleme sürümlerini de göster
        public string Window = "";      // x,y,w,h,maximized

        public static string Folder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrokenHub"); }
        }

        static string FilePath
        {
            get
            {
                Directory.CreateDirectory(Folder);
                return Path.Combine(Folder, "settings.txt");
            }
        }

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                    switch (k)
                    {
                        case "lang": s.Lang = v; break;
                        case "previews": s.Previews = v == "1"; break;
                        case "window": s.Window = v; break;
                    }
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, new[] { "lang=" + Lang, "previews=" + (Previews ? "1" : "0"), "window=" + Window });
            }
            catch { }
        }
    }
}
