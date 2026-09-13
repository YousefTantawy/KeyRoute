using System.Windows.Forms;

namespace KeyRoute
{
    public class SubstituteKeyDialog : Form
    {
        private readonly ComboBox _combo;

        public Keys SelectedKey { get; private set; }

        public SubstituteKeyDialog(Keys physicalKey, List<Keys> availableKeys, Keys? current)
        {
            Text = "Choose substitute key";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(300, 170);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9F);

            var label = new Label
            {
                Text = $"{physicalKey} sends:",
                AutoSize = true,
                ForeColor = Theme.Text,
                Location = new Point(16, 20)
            };

            _combo = Theme.CreateComboBox(16, 46, 250);
            _combo.Items.AddRange(availableKeys.Cast<object>().ToArray());
            _combo.SelectedItem = current is not null && availableKeys.Contains(current.Value)
                ? current.Value
                : availableKeys[0];

            var okButton = Theme.CreateButton("OK", 94, 90, 84);
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += (_, _) => SelectedKey = (Keys)_combo.SelectedItem!;

            var cancelButton = Theme.CreateButton("Cancel", 182, 90, 84);
            cancelButton.DialogResult = DialogResult.Cancel;

            Controls.Add(label);
            Controls.Add(_combo);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
