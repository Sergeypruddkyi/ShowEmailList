using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShowEmailList
{
    // One account block: address on top, password below it, both visually
    // grouped by a single bordered panel. Icon buttons (copy / reveal /
    // delete) are right-aligned on their own rows. Manual layout in the
    // reference style; the block reflows its children on resize.
    public class AccountRow : Panel
    {
        private const int BaseH = 88;   // pad*2 + field*2 + vgap
        private const int Btn = 30;
        private const int Pad = 10;
        private const int Gap = 6;      // field -> its buttons
        private const int VGap = 8;     // address row -> password row

        private const string GlyphCopy = "\uE8C8";    // Copy (two squares)
        private const string GlyphShow = "\uE890";    // View (eye)
        private const string GlyphHide = "\uED1A";    // Hide (eye with slash)
        private const string GlyphDelete = "\uE74D";  // Delete (trash)

        // Slightly lighter than the window background so the block reads as
        // one highlighted group around the two fields.
        private static readonly Color BlockBack = Color.FromArgb(37, 37, 41);

        public readonly TextBox LoginBox;
        public readonly TextBox PasswordBox;

        private readonly Button _copyLoginBtn;
        private readonly Button _eyeBtn;
        private readonly Button _copyPassBtn;
        private readonly Button _deleteBtn;
        private readonly Font _glyphFont;
        private readonly float _k;
        private bool _revealed;

        public event EventHandler ValuesChanged;
        public event EventHandler DeleteRequested;

        public AccountRow(string login, string password, float scale)
        {
            _k = scale;
            DoubleBuffered = true;
            BackColor = BlockBack;
            Height = S(BaseH);

            LoginBox = new TextBox();
            LoginBox.Text = login ?? "";
            LoginBox.PlaceholderText = "Email address";
            LoginBox.Font = Theme.Regular;
            Theme.StyleInput(LoginBox);
            LoginBox.TextChanged += delegate { RaiseChanged(); };
            Controls.Add(LoginBox);

            PasswordBox = new TextBox();
            PasswordBox.Text = password ?? "";
            PasswordBox.PlaceholderText = "Password";
            PasswordBox.Font = Theme.Regular;
            PasswordBox.PasswordChar = '*';
            Theme.StyleInput(PasswordBox);
            PasswordBox.TextChanged += delegate { RaiseChanged(); };
            Controls.Add(PasswordBox);

            _glyphFont = new Font("Segoe MDL2 Assets", 9.5F * Math.Max(1f, scale));

            _copyLoginBtn = MakeButton(GlyphCopy, "Copy email", delegate { CopyText(LoginBox.Text); });
            _eyeBtn = MakeButton(GlyphShow, "Show password", delegate { ToggleReveal(); });
            _copyPassBtn = MakeButton(GlyphCopy, "Copy password", delegate { CopyText(PasswordBox.Text); });
            _deleteBtn = MakeButton(GlyphDelete, "Delete account", delegate
            {
                EventHandler h = DeleteRequested;
                if (h != null) h(this, EventArgs.Empty);
            });
            _deleteBtn.MouseEnter += delegate { _deleteBtn.BackColor = Color.FromArgb(196, 43, 28); };
            _deleteBtn.MouseLeave += delegate { _deleteBtn.BackColor = Theme.Button; };
            Controls.Add(_copyLoginBtn);
            Controls.Add(_eyeBtn);
            Controls.Add(_copyPassBtn);
            Controls.Add(_deleteBtn);

            LayoutChildren();
        }

        private Button MakeButton(string glyph, string accName, EventHandler onClick)
        {
            Button b = new Button();
            b.Text = glyph;
            b.Font = _glyphFont;
            b.AccessibleName = accName;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Theme.Button;
            b.ForeColor = Theme.Light;
            b.TabStop = false;
            b.MouseEnter += delegate { if (b != _deleteBtn) b.BackColor = Theme.ButtonHover; };
            b.MouseLeave += delegate { if (b != _deleteBtn) b.BackColor = Theme.Button; };
            b.Click += onClick;
            return b;
        }

        private void ToggleReveal()
        {
            _revealed = !_revealed;
            PasswordBox.PasswordChar = _revealed ? '\0' : '*';
            _eyeBtn.Text = _revealed ? GlyphHide : GlyphShow;
            _eyeBtn.AccessibleName = _revealed ? "Hide password" : "Show password";
        }

        private static void CopyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { Clipboard.SetText(text); } catch { }
        }

        private void RaiseChanged()
        {
            EventHandler h = ValuesChanged;
            if (h != null) h(this, EventArgs.Empty);
        }

        private int S(int v)
        {
            return (int)Math.Round(v * _k);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            // Height is set before the fields exist in the ctor; OnResize
            // fires on that assignment, so bail until the controls are there.
            if (LoginBox == null || PasswordBox == null || _copyLoginBtn == null) return;
            int fieldH = Math.Max(LoginBox.Height, 1);
            int pad = S(Pad), gap = S(Gap), btn = S(Btn), vgap = S(VGap);
            int right = Width - pad;

            // Row 1: address field + copy button at the far right.
            _copyLoginBtn.SetBounds(right - btn, pad, btn, fieldH);
            LoginBox.SetBounds(pad, pad, right - btn - gap - pad, fieldH);

            // Row 2: password field + eye, copy, delete at the far right.
            int y2 = pad + fieldH + vgap;
            _deleteBtn.SetBounds(right - btn, y2, btn, fieldH);
            _copyPassBtn.SetBounds(right - btn * 2 - gap, y2, btn, fieldH);
            _eyeBtn.SetBounds(right - btn * 3 - gap * 2, y2, btn, fieldH);
            PasswordBox.SetBounds(pad, y2, right - btn * 3 - gap * 3 - pad, fieldH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Theme.Border))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_glyphFont != null) _glyphFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
