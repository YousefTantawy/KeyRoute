using System.Windows.Forms;

namespace KeyRoute
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(30, 30, 30);
        public static readonly Color Panel = Color.FromArgb(37, 37, 38);
        public static readonly Color Control = Color.FromArgb(62, 62, 66);
        public static readonly Color Accent = Color.FromArgb(0, 122, 204);
        public static readonly Color Text = Color.FromArgb(241, 241, 241);
        public static readonly Color SubtleText = Color.FromArgb(153, 153, 153);

        public static Button CreateButton(string text, int x, int y, int width = 126, int height = 30)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Control,
                ForeColor = Text,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = Control;
            button.FlatAppearance.MouseOverBackColor = Accent;
            return button;
        }

        public static ComboBox CreateComboBox(int x, int y, int width)
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(x, y),
                Size = new Size(width, 24),
                BackColor = Control,
                ForeColor = Text,
                FlatStyle = FlatStyle.Flat
            };
        }
    }
}
