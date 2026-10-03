using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShowEmailList
{
    // Dark-styled modal dialog in the spirit of the reference's PromptForm.
    // The dialog sizes itself to its content: the message wraps at a fixed
    // text width and the buttons are as wide as their captions need.
    public class PromptForm : Form
    {
        public string Chosen;

        private PromptForm(string title, string message, string[] buttons, bool warning)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Theme.Back;
            Font = Theme.Regular;

            const int pad = 16;       // outer margin
            const int textMaxW = 340; // message wrap threshold
            const int btnH = 30;
            const int btnPad = 24;    // extra width inside a button
            const int gap = 8;        // spacing between buttons
            const int gapTextBtn = 18;

            Label lbl = new Label();
            lbl.Text = message;
            lbl.ForeColor = warning ? Theme.Red : Theme.Light;
            Size ts = TextRenderer.MeasureText(message, Theme.Regular, new Size(textMaxW, 10000), TextFormatFlags.WordBreak);
            int lblW = Math.Min(ts.Width + 4, textMaxW);
            int lblH = ts.Height + 4;
            lbl.Location = new Point(pad, pad);
            lbl.Size = new Size(lblW, lblH);
            Controls.Add(lbl);

            int contentW = lblW;
            Size[] btnSizes = new Size[buttons.Length];
            int totalW = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                int w = TextRenderer.MeasureText(buttons[i], Theme.Regular).Width + btnPad;
                btnSizes[i] = new Size(w, btnH);
                totalW += w;
            }
            totalW += gap * (buttons.Length - 1);
            if (totalW > contentW) contentW = totalW;

            int btnY = pad + lblH + gapTextBtn;
            int clientW = contentW + pad * 2;
            int clientH = btnY + btnH + pad;
            ClientSize = new Size(clientW, clientH);

            int x = clientW - pad;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                Button btn = new Button();
                btn.Text = buttons[i];
                x -= btnSizes[i].Width;
                btn.Location = new Point(x, btnY);
                btn.Size = btnSizes[i];
                Theme.StyleButton(btn);
                string chosen = buttons[i];
                btn.Click += delegate { Chosen = chosen; Close(); };
                Controls.Add(btn);
                x -= gap;
            }

            float k = ChromeApi.GetDpiForWindowAt(Cursor.Position) / 96f;
            if (k > 0.999f && k < 1.001f) k = 1f;
            if (k != 1f) Scale(new SizeF(k, k));
        }

        public static string Show(Form owner, string title, string message, bool warning, params string[] buttons)
        {
            using (PromptForm f = new PromptForm(title, message, buttons, warning))
            {
                if (owner == null || owner.IsDisposed)
                {
                    if (Application.OpenForms.Count > 0) owner = Application.OpenForms[0];
                }
                if (owner != null && !owner.IsDisposed) f.ShowDialog(owner);
                else f.ShowDialog();
                return f.Chosen;
            }
        }
    }
}
