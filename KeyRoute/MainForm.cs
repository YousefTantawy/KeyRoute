using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyRoute
{
    public class MainForm : Form
    {
        private readonly KeyboardHook _hook;
        private readonly ConfigStore _store;
        private readonly List<KeyMapping> _mappings;

        private readonly ListView _listView;
        private readonly Button _addButton;
        private readonly Button _removeButton;
        private readonly Label _statusLabel;

        private bool _capturing;

        public MainForm(KeyboardHook hook, ConfigStore store, List<KeyMapping> mappings)
        {
            _hook = hook;
            _store = store;
            _mappings = mappings;

            Text = "KeyRoute";
            Size = new Size(460, 420);
            MinimumSize = new Size(380, 320);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            var title = new Label
            {
                Text = "Key Mappings",
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 14)
            };

            var subtitle = new Label
            {
                Text = "Reclaimed keys are blocked everywhere and sent only as their substitute. Double-click a row to change its substitute.",
                ForeColor = Theme.SubtleText,
                AutoSize = true,
                MaximumSize = new Size(412, 0),
                Location = new Point(16, 42)
            };

            _listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = Theme.Panel,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 70),
                Size = new Size(412, 230),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _listView.Columns.Add("Physical key", 200);
            _listView.Columns.Add("Sends", 190);
            _listView.MouseDoubleClick += OnListViewDoubleClick;
            TryApplyDarkListView(_listView);

            _addButton = Theme.CreateButton("Add mapping", 16, 312);
            _addButton.Click += OnAddClicked;

            _removeButton = Theme.CreateButton("Remove selected", 150, 312);
            _removeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _removeButton.Click += OnRemoveClicked;

            _statusLabel = new Label
            {
                ForeColor = Theme.SubtleText,
                AutoSize = true,
                Location = new Point(16, 352),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _addButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(_listView);
            Controls.Add(_addButton);
            Controls.Add(_removeButton);
            Controls.Add(_statusLabel);

            KeyDown += OnFormKeyDown;

            RefreshList();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TryEnableDarkTitleBar();
        }

        private void RefreshList()
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();
            foreach (var mapping in _mappings)
            {
                var item = new ListViewItem(mapping.PhysicalKey.ToString())
                {
                    Tag = mapping,
                    UseItemStyleForSubItems = true,
                    BackColor = Theme.Panel,
                    ForeColor = Theme.Text
                };
                item.SubItems.Add(mapping.SubstituteKey.ToString());
                _listView.Items.Add(item);
            }
            _listView.EndUpdate();

            _statusLabel.Text = $"{_mappings.Count} / {SubstituteKeyPool.All.Length} substitute keys used";
        }

        private void OnAddClicked(object? sender, EventArgs e)
        {
            if (_capturing)
                return;

            if (SubstituteKeyPool.NextAvailable(_mappings) is null)
            {
                MessageBox.Show(this, "All 12 substitute keys (F13–F24) are already assigned.",
                    "No slots available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _capturing = true;
            _addButton.Text = "Press a key… (Esc to cancel)";
        }

        private void OnRemoveClicked(object? sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
                return;

            var mapping = (KeyMapping)_listView.SelectedItems[0].Tag!;
            _mappings.Remove(mapping);
            Persist();
        }

        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_capturing)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            EndCapture();

            if (e.KeyCode == Keys.Escape)
                return;

            var key = e.KeyCode;

            if (SubstituteKeyPool.All.Contains(key))
            {
                MessageBox.Show(this, "F13–F24 are substitute keys and can't be reclaimed themselves.",
                    "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_mappings.Any(m => m.PhysicalKey == key))
            {
                MessageBox.Show(this, $"{key} is already mapped.",
                    "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var substitute = PromptForSubstitute(key, currentSubstitute: null);
            if (substitute is null)
                return;

            _mappings.Add(new KeyMapping { PhysicalKey = key, SubstituteKey = substitute.Value });
            Persist();
        }

        private void OnListViewDoubleClick(object? sender, MouseEventArgs e)
        {
            var item = _listView.GetItemAt(e.X, e.Y);
            if (item is null)
                return;

            var mapping = (KeyMapping)item.Tag!;
            var substitute = PromptForSubstitute(mapping.PhysicalKey, mapping.SubstituteKey);
            if (substitute is null || substitute == mapping.SubstituteKey)
                return;

            mapping.SubstituteKey = substitute.Value;
            Persist();
        }

        private Keys? PromptForSubstitute(Keys physicalKey, Keys? currentSubstitute)
        {
            var usedByOthers = _mappings
                .Where(m => m.PhysicalKey != physicalKey)
                .Select(m => m.SubstituteKey)
                .ToHashSet();

            var available = SubstituteKeyPool.All.Where(k => !usedByOthers.Contains(k)).ToList();
            if (available.Count == 0)
            {
                MessageBox.Show(this, "All 12 substitute keys (F13–F24) are already assigned.",
                    "No slots available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            using var dialog = new SubstituteKeyDialog(physicalKey, available, currentSubstitute);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedKey : null;
        }

        private void EndCapture()
        {
            _capturing = false;
            _addButton.Text = "Add mapping";
        }

        private void Persist()
        {
            _hook.UpdateMappings(_mappings);
            _store.Save(_mappings);
            RefreshList();
        }

        private static void TryApplyDarkListView(Control control)
        {
            try { SetWindowTheme(control.Handle, "DarkMode_Explorer", null); }
            catch { /* best-effort visual polish only */ }
        }

        private void TryEnableDarkTitleBar()
        {
            try
            {
                int useDarkMode = 1;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            }
            catch { /* best-effort visual polish only */ }
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);
    }
}
