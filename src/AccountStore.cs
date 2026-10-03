using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Forms;

namespace ShowPost
{
    public class AccountEntry
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    // Portable JSON store for email accounts (accounts.json next to the exe).
    // Same durability mechanic as Settings: write to .tmp, then Replace.
    public class AccountStore
    {
        private readonly string _path;
        private static bool _loadFailedWarned;
        private static bool _saveFailedWarned;

        public readonly List<AccountEntry> Accounts = new List<AccountEntry>();

        public AccountStore(string path)
        {
            _path = path;
        }

        public void Load()
        {
            Accounts.Clear();
            try
            {
                if (!File.Exists(_path)) return;
                string json = File.ReadAllText(_path, Encoding.UTF8);
                List<AccountEntry> loaded = JsonSerializer.Deserialize<List<AccountEntry>>(json);
                if (loaded == null) return;
                foreach (AccountEntry e in loaded)
                {
                    if (e == null) continue;
                    AccountEntry entry = new AccountEntry();
                    entry.Login = e.Login ?? "";
                    entry.Password = e.Password ?? "";
                    Accounts.Add(entry);
                }
            }
            catch (Exception ex)
            {
                // Keep the unreadable file for the user instead of overwriting it.
                try { File.Move(_path, _path + ".bad", true); } catch { }
                WarnOnce(ref _loadFailedWarned, "Не удалось прочитать accounts.json:\n" + ex.Message + "\n\nФайл сохранён как accounts.json.bad, список начат с пустого.");
            }
        }

        public void Save()
        {
            string tmp = _path + ".tmp";
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(Accounts, options);
                try
                {
                    File.WriteAllText(tmp, json, new UTF8Encoding(false));
                    if (File.Exists(_path)) File.Replace(tmp, _path, null);
                    else File.Move(tmp, _path);
                }
                catch (IOException)
                {
                    File.WriteAllText(_path, json, new UTF8Encoding(false));
                }
                finally
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }
            }
            catch (Exception ex)
            {
                WarnOnce(ref _saveFailedWarned, "Не удалось сохранить accounts.json:\n" + ex.Message);
            }
        }

        private static void WarnOnce(ref bool warned, string message)
        {
            if (warned) return;
            warned = true;
            try
            {
                MessageBox.Show(message, "ShowPost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch { }
        }
    }
}
