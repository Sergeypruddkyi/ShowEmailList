using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ShowPost
{
    public class MainForm : Form
    {
        private const int BaseWidth = 500;

        private const int BarBaseHeight = 64;
        private const int RowGap = 8;
        private const int ListTopPad = 8;
        private const int ListSidePad = 20;
        private const int RowBaseHeight = 88;

        private readonly Settings _settings = new Settings(AppPaths.SettingsPath);
        private readonly AccountStore _accounts = new AccountStore(AppPaths.AccountsPath);
        private readonly List<AccountRow> _rows = new List<AccountRow>();
        private readonly Timer _saveTimer = new Timer();

        private Panel content;
        private DarkTitleBar titleBar;
        private Panel list;
        private Panel bottomBar;
        private Button addBtn;
        private Label lblCount;

        private float _dpiScale = 1f;
        private bool _regionFallback;

        public MainForm()
        {
            _settings.Load();
            _accounts.Load();
            Text = "ShowPost";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }
            FormBorderStyle = FormBorderStyle.None;
            Font = Theme.Regular;
            BackColor = Theme.Back;
            MaximizeBox = false;

            // Provisional size; the real height is content-driven and is
            // applied once the rows exist (ApplyDynamicLayout).
            ClientSize = new Size(BaseWidth, 300);
            MinimumSize = new Size(BaseWidth, 200);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();

            // Debounced autosave: every text change restarts the timer, the
            // tick performs the actual write. Add/delete save immediately.
            _saveTimer.Interval = 800;
            _saveTimer.Tick += delegate
            {
                _saveTimer.Stop();
                SaveAll();
            };

            // DPI scaling is applied BEFORE the handle is created: after
            // CreateHandle the first WM_NCCALCSIZE (borderless chrome) distorts
            // ClientSize, and the content anchors capture wrong distances.
            _dpiScale = ChromeApi.GetDpiForWindowAt(Cursor.Position) / 96f;
            if (_dpiScale > 0.999f && _dpiScale < 1.001f) _dpiScale = 1f;
            if (_dpiScale != 1f)
            {
                Scale(new SizeF(_dpiScale, _dpiScale));
            }

            foreach (AccountEntry entry in _accounts.Accounts)
                AddRow(entry.Login, entry.Password, false);
            if (_rows.Count == 0) AddAccount();
            UpdateCount();
            ApplyDynamicLayout();
        }

        private void BuildUi()
        {
            content = new ChromePanel(this);
            content.Location = new Point(0, DarkTitleBar.BarHeight);
            content.Size = new Size(ClientSize.Width, ClientSize.Height - DarkTitleBar.BarHeight);
            content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            content.BackColor = Theme.Back;
            Controls.Add(content);

            list = new Panel();
            list.Location = new Point(0, 0);
            list.Size = new Size(content.Width, content.Height - BarBaseHeight);
            list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            list.BackColor = Theme.Back;
            list.AutoScroll = true;
            list.SizeChanged += delegate { LayoutRowsWidth(); };
            content.Controls.Add(list);

            bottomBar = new Panel();
            bottomBar.Location = new Point(0, content.Height - BarBaseHeight);
            bottomBar.Size = new Size(content.Width, BarBaseHeight);
            bottomBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottomBar.BackColor = Theme.Back;
            bottomBar.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.Border))
                    e.Graphics.DrawLine(p, 0, 0, bottomBar.Width, 0);
            };
            content.Controls.Add(bottomBar);

            addBtn = new Button();
            addBtn.Text = "Добавить аккаунт";
            addBtn.Size = new Size(190, 36);
            addBtn.Location = new Point(S(ListSidePad), S(14));
            addBtn.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            Theme.StyleAccent(addBtn);
            addBtn.Click += delegate { AddAccount(); };
            bottomBar.Controls.Add(addBtn);

            lblCount = new Label();
            lblCount.Text = "Аккаунтов: 0";
            lblCount.ForeColor = Theme.Dim;
            lblCount.AutoSize = true;
            lblCount.BackColor = Theme.Back;
            lblCount.TextChanged += delegate { PositionCount(); };
            bottomBar.Controls.Add(lblCount);
            bottomBar.SizeChanged += delegate { PositionCount(); };

            titleBar = new DarkTitleBar(this);
            Controls.Add(titleBar);
            titleBar.BringToFront();
        }

        private int S(int v)
        {
            return (int)Math.Round(v * _dpiScale);
        }

        // The window is exactly as tall as its content: title bar + rows +
        // bottom bar. It grows downward as blocks are added and shrinks when
        // they are removed. If the content outgrows the monitor work area the
        // window stops growing and the list starts scrolling.
        private void ApplyDynamicLayout()
        {
            int barH = S(BarBaseHeight);
            int rowsH = S(ListTopPad);
            for (int i = 0; i < _rows.Count; i++)
                rowsH += _rows[i].Height + S(RowGap);
            if (_rows.Count > 0) rowsH += S(ListTopPad) - S(RowGap);

            int minH = DarkTitleBar.BarHeight + S(ListTopPad * 2 + RowBaseHeight) + barH;
            MinimumSize = new Size(S(BaseWidth), minH);

            int desired = DarkTitleBar.BarHeight + rowsH + barH;
            int winH = Math.Min(Math.Max(desired, minH), Math.Max(minH, MaxWorkHeight()));

            ClientSize = new Size(ClientSize.Width, winH);
            list.Height = Math.Max(1, content.Height - barH);
            bottomBar.Height = barH;
            bottomBar.Top = content.Height - barH;

            if (IsHandleCreated)
            {
                ChromeApi.MonitorInfo mi = WindowMonitorInfo();
                if (mi.CbSize != 0 && Top + Height > mi.Work.Bottom)
                    Top = Math.Max(mi.Work.Top, mi.Work.Bottom - Height);
            }
        }

        private ChromeApi.MonitorInfo WindowMonitorInfo()
        {
            ChromeApi.MonitorInfo mi = new ChromeApi.MonitorInfo();
            mi.CbSize = (uint)Marshal.SizeOf(typeof(ChromeApi.MonitorInfo));
            IntPtr mon;
            if (IsHandleCreated)
                mon = ChromeApi.MonitorFromWindow(Handle, ChromeApi.MonitorDefaultToNearest);
            else
                mon = ChromeApi.MonitorFromPoint(
                    new ChromeApi.ChromePoint { X = Cursor.Position.X, Y = Cursor.Position.Y },
                    ChromeApi.MonitorDefaultToNearest);
            if (mon != IntPtr.Zero && ChromeApi.GetMonitorInfo(mon, ref mi))
                return mi;
            return mi;
        }

        private int MaxWorkHeight()
        {
            ChromeApi.MonitorInfo mi = WindowMonitorInfo();
            if (mi.CbSize != 0 && mi.Work.Bottom > mi.Work.Top)
                return mi.Work.Bottom - mi.Work.Top;
            return 1200;
        }

        private int RowWidth()
        {
            int vscroll = SystemInformation.VerticalScrollBarWidth;
            return Math.Max(240, list.ClientSize.Width - S(ListSidePad + 10) - vscroll);
        }

        private void LayoutRowsWidth()
        {
            int w = RowWidth();
            foreach (AccountRow row in _rows)
                row.Width = w;
        }

        private void ReflowRows()
        {
            int step = (_rows.Count > 0 ? _rows[0].Height : S(RowBaseHeight)) + S(RowGap);
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].Location = new Point(S(ListSidePad), S(ListTopPad) + i * step);
        }

        private void AddAccount()
        {
            AccountEntry entry = new AccountEntry();
            entry.Login = "";
            entry.Password = "";
            _accounts.Accounts.Add(entry);
            AddRow("", "", true);
        }

        private void AddRow(string login, string password, bool saveAndScroll)
        {
            AccountRow row = new AccountRow(login, password, _dpiScale);
            int i = _rows.Count;
            row.Width = RowWidth();
            row.Location = new Point(S(ListSidePad), S(ListTopPad) + i * (row.Height + S(RowGap)));
            row.ValuesChanged += delegate { SaveSoon(); };
            row.DeleteRequested += delegate { DeleteRow(row); };
            _rows.Add(row);
            list.Controls.Add(row);
            if (saveAndScroll)
            {
                list.ScrollControlIntoView(row);
                SaveAll();
            }
            UpdateCount();
            ApplyDynamicLayout();
        }

        private void DeleteRow(AccountRow row)
        {
            int i = _rows.IndexOf(row);
            if (i < 0) return;
            string chosen = PromptForm.Show(this, "Удаление", "Удалить этот блок аккаунта?", true, "Удалить", "Отмена");
            if (chosen != "Удалить") return;
            _rows.RemoveAt(i);
            _accounts.Accounts.RemoveAt(i);
            list.Controls.Remove(row);
            row.Dispose();
            ReflowRows();
            SaveAll();
            UpdateCount();
            ApplyDynamicLayout();
        }

        private void SaveSoon()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveAll()
        {
            for (int i = 0; i < _rows.Count && i < _accounts.Accounts.Count; i++)
            {
                _accounts.Accounts[i].Login = _rows[i].LoginBox.Text;
                _accounts.Accounts[i].Password = _rows[i].PasswordBox.Text;
            }
            _accounts.Save();
        }

        private void UpdateCount()
        {
            lblCount.Text = "Аккаунтов: " + _rows.Count;
        }

        private void PositionCount()
        {
            lblCount.Left = Math.Max(S(ListSidePad), bottomBar.ClientSize.Width - lblCount.Width - S(ListSidePad));
            lblCount.Top = Math.Max(0, (bottomBar.Height - lblCount.Height) / 2);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyChromeStyles();
            ApplyRoundedCorners();
            ApplyDynamicLayout();
            PositionCount();
        }

        // Win11: ask DWM for system-rounded corners (smooth, matches other
        // windows). On Win10 that attribute is unsupported, so clip the
        // window to a rounded region instead and keep it updated on resize.
        private void ApplyRoundedCorners()
        {
            int pref = ChromeApi.DwmwcpRound;
            int hr = ChromeApi.DwmSetWindowAttribute(Handle, ChromeApi.DwmwaWindowCornerPreference, ref pref, 4);
            if (hr != 0)
            {
                _regionFallback = true;
                UpdateWindowRegion();
            }
        }

        private void UpdateWindowRegion()
        {
            int r = S(8);
            IntPtr hrgn = ChromeApi.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, r, r);
            ChromeApi.SetWindowRgn(Handle, hrgn, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_regionFallback) UpdateWindowRegion();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_saveTimer.Enabled)
            {
                _saveTimer.Stop();
                SaveAll();
            }
        }

        // Keep the WS_THICKFRAME / WS_CAPTION / etc. style bits on the
        // borderless window so the OS still gives us resize, maximize and
        // snap behaviour while we draw the chrome ourselves.
        private void ApplyChromeStyles()
        {
            int style = ChromeApi.GetWindowLong(Handle, ChromeApi.GwlStyle);
            int desired = style | 0x00040000 | 0x00C00000 | 0x00020000 | 0x00010000 | 0x00080000;
            if (desired != style)
            {
                ChromeApi.SetWindowLong(Handle, ChromeApi.GwlStyle, desired);
                ChromeApi.SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ChromeApi.WmDpiChanged)
            {
                HandleDpiChanged(ref m);
                return;
            }
            if (m.Msg == ChromeApi.WmGetMinMaxInfo)
            {
                // Maximize to the monitor's WORK area (taskbar stays visible).
                base.WndProc(ref m);
                IntPtr mon = ChromeApi.MonitorFromWindow(m.HWnd, ChromeApi.MonitorDefaultToNearest);
                ChromeApi.MonitorInfo mi = new ChromeApi.MonitorInfo();
                mi.CbSize = (uint)Marshal.SizeOf(typeof(ChromeApi.MonitorInfo));
                if (mon != IntPtr.Zero && ChromeApi.GetMonitorInfo(mon, ref mi))
                {
                    Marshal.WriteInt32(m.LParam, 8, mi.Work.Right - mi.Work.Left);
                    Marshal.WriteInt32(m.LParam, 12, mi.Work.Bottom - mi.Work.Top);
                    Marshal.WriteInt32(m.LParam, 16, mi.Work.Left);
                    Marshal.WriteInt32(m.LParam, 20, mi.Work.Top);
                }
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == ChromeApi.WmNcCalcSize && m.WParam != IntPtr.Zero)
            {
                // Remove the system non-client frame entirely.
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == ChromeApi.WmNcHitTest)
            {
                // Height is content-driven, so only horizontal resize is
                // offered: the left/right edges. The top strip below the
                // title bar falls through to the HTCAPTION drag area.
                int sx = unchecked((short)((long)m.LParam & 0xFFFF));
                int sy = unchecked((short)(((long)m.LParam >> 16) & 0xFFFF));
                Point cpt = PointToClient(new Point(sx, sy));
                bool maximized = ChromeApi.IsZoomedStyle(m.HWnd);
                if (!maximized)
                {
                    int fw = ChromeApi.GetSystemMetrics(ChromeApi.SmCxSizeFrame) + ChromeApi.GetSystemMetrics(ChromeApi.SmCxPaddedBorder);
                    if (cpt.X < fw) { m.Result = (IntPtr)ChromeApi.HtLeft; return; }
                    if (cpt.X >= ClientSize.Width - fw) { m.Result = (IntPtr)ChromeApi.HtRight; return; }
                }
                // Dragging the title bar moves the window.
                if (titleBar != null && cpt.Y < titleBar.Height) { m.Result = (IntPtr)ChromeApi.HtCaption; return; }
                m.Result = (IntPtr)ChromeApi.HtClient;
                return;
            }
            base.WndProc(ref m);
        }

        private void HandleDpiChanged(ref Message m)
        {
            long wparam = m.WParam.ToInt64();
            int dpiX = (int)(wparam & 0xFFFF);
            int dpiY = (int)((wparam >> 16) & 0xFFFF);
            float newScale = Math.Max(dpiX, dpiY) / 96f;
            if (newScale < 0.5f || newScale > 8f) { m.Result = IntPtr.Zero; return; }
            ChromeApi.ChromeRect proposed = (ChromeApi.ChromeRect)Marshal.PtrToStructure(m.LParam, typeof(ChromeApi.ChromeRect));
            float oldScale = _dpiScale;
            _dpiScale = newScale;
            try
            {
                Bounds = new Rectangle(proposed.Left, proposed.Top,
                    proposed.Right - proposed.Left, proposed.Bottom - proposed.Top);
                if (Math.Abs(newScale - oldScale) > 0.001f && oldScale > 0.001f)
                {
                    float factor = newScale / oldScale;
                    Scale(new SizeF(factor, factor));
                }
                LayoutRowsWidth();
                ReflowRows();
                PositionCount();
                ApplyDynamicLayout();
            }
            catch { }
            m.Result = IntPtr.Zero;
        }
    }
}
