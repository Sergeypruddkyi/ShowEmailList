using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShowPost
{
    // Simple INI-backed settings store (key=value under a [General] header).
    // Mechanic taken from the YouTubeDownloader-Net10 reference, minus its
    // downloader-specific properties.
    public class Settings
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Settings(string path)
        {
            _path = path;
        }

        public string Get(string key)
        {
            string v;
            return _values.TryGetValue(key, out v) ? v : null;
        }

        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) _values.Remove(key);
            else _values[key] = value;
        }

        private static bool _loadFailedWarned;
        private static bool _saveFailedWarned;

        private static void WarnOnce(ref bool warned, string action, Exception ex)
        {
            if (warned) return;
            warned = true;
            try
            {
                System.Windows.Forms.MessageBox.Show("Failed to " + action + " settings:\n" + ex.Message, "ShowPost", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch { }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                string[] lines = File.ReadAllLines(_path, Encoding.UTF8);
                _values.Clear();
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("[")) continue;
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    _values[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
            }
            catch (Exception ex)
            {
                WarnOnce(ref _loadFailedWarned, "load", ex);
            }
        }

        public void Save()
        {
            string tmp = _path + ".tmp";
            string bak = _path + ".bak";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[General]");
                foreach (KeyValuePair<string, string> kv in _values)
                    sb.AppendLine(kv.Key + "=" + kv.Value);

                string content = sb.ToString();
                try
                {
                    File.WriteAllText(tmp, content, Encoding.UTF8);
                    if (File.Exists(_path))
                    {
                        try
                        {
                            File.Replace(tmp, _path, bak, true);
                        }
                        catch (IOException)
                        {
                            try { if (File.Exists(bak)) File.Delete(bak); }
                            catch { }
                            File.WriteAllText(_path, content, Encoding.UTF8);
                        }
                    }
                    else
                    {
                        File.Move(tmp, _path);
                    }
                }
                finally
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); }
                    catch { }
                    try { if (File.Exists(bak)) File.Delete(bak); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                WarnOnce(ref _saveFailedWarned, "save", ex);
            }
        }
    }
}
