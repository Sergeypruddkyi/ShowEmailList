using System.Drawing;
using System.Windows.Forms;

namespace ShowEmailList
{
    internal static class Theme
    {
        public const float BaseSize = 11.5F;

        public static readonly Font Regular = new Font("Segoe UI", BaseSize);
        public static readonly Font BoldStatus = new Font("Segoe UI", BaseSize, FontStyle.Bold);
        public static readonly Font Log = new Font("Consolas", 10.5F);
        public static readonly Font Big = new Font("Segoe UI", 13F);

        public static readonly Color Back = Color.FromArgb(30, 30, 30);
        public static readonly Color Input = Color.FromArgb(45, 45, 48);
        public static readonly Color Button = Color.FromArgb(51, 51, 55);
        public static readonly Color ButtonHover = Color.FromArgb(63, 63, 70);
        public static readonly Color Border = Color.FromArgb(63, 63, 70);
        public static readonly Color Light = Color.FromArgb(225, 225, 225);
        public static readonly Color Dim = Color.FromArgb(154, 154, 154);
        public static readonly Color Green = Color.FromArgb(108, 203, 108);
        public static readonly Color Red = Color.FromArgb(255, 107, 107);
        public static readonly Color Orange = Color.FromArgb(255, 184, 77);
        public static readonly Color Accent = Color.FromArgb(0, 120, 212);

        public static void StyleButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = ButtonHover;
            b.BackColor = Button;
            b.ForeColor = Light;
        }

        public static void StyleAccent(Button b)
        {
            StyleButton(b);
            b.BackColor = Accent;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 135, 230);
        }

        public static void StyleInput(TextBox t)
        {
            t.BackColor = Input;
            t.ForeColor = Light;
            t.BorderStyle = BorderStyle.FixedSingle;
        }
    }
}
