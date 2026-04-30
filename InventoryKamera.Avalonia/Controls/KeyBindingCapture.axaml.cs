using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace InventoryKamera.Avalonia.Controls
{
    /// <summary>
    /// A focusable control that captures the next key press and exposes it as an
    /// <c>int</c> (<see cref="Avalonia.Input.Key"/> cast to int).
    ///
    /// Bind <see cref="KeyValue"/> two-way from a ViewModel property.
    /// The displayed string is derived from <see cref="KeyValue"/> via
    /// <c>((Key)KeyValue).ToString()</c> — e.g. "B", "Up", "F1".
    ///
    /// When the control is focused and waiting for input it shows "Press a key…".
    /// </summary>
    public partial class KeyBindingCapture : UserControl
    {
        // ─────────────────────────────────────────────────────────────────────
        // Brushes — inline theme colours (matching GenshinTheme.axaml)
        // Using hard-coded fallbacks avoids dynamic-resource-lookup plumbing
        // inside a code-behind constructor where the resource tree may not yet
        // be fully attached.
        // ─────────────────────────────────────────────────────────────────────

        private static readonly SolidColorBrush AccentColour =
            new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2));

        private static readonly SolidColorBrush TextSecondaryColour =
            new SolidColorBrush(Color.FromRgb(0xA6, 0x9F, 0x92));

        private static readonly SolidColorBrush TextPrimaryColour =
            new SolidColorBrush(Color.FromRgb(0xEC, 0xE5, 0xD8));

        // Subtler border for the unfocused state — matches the Item Filters / Output text boxes.
        private static readonly SolidColorBrush UnfocusedBorderColour =
            new SolidColorBrush(Color.FromRgb(0x55, 0x59, 0x60));

        // ─────────────────────────────────────────────────────────────────────
        // Styled properties
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The captured Avalonia <see cref="Key"/> value cast to <c>int</c>.
        /// Zero (Key.None) means nothing has been captured yet.
        /// </summary>
        public static readonly StyledProperty<int> KeyValueProperty =
            AvaloniaProperty.Register<KeyBindingCapture, int>(
                nameof(KeyValue),
                defaultValue: 0);

        public int KeyValue
        {
            get => GetValue(KeyValueProperty);
            set => SetValue(KeyValueProperty, value);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Read-only derived display string
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable name for the captured key.
        /// Returns an empty string when <see cref="KeyValue"/> is 0 (Key.None).
        /// </summary>
        public string KeyDisplay => KeyValueToDisplay(KeyValue);

        // ─────────────────────────────────────────────────────────────────────
        // Construction
        // ─────────────────────────────────────────────────────────────────────

        public KeyBindingCapture()
        {
            InitializeComponent();

            // React to focus changes and KeyDown from the inner Border
            CaptureRoot.GotFocus      += OnCaptureGotFocus;
            CaptureRoot.LostFocus     += OnCaptureLostFocus;
            CaptureRoot.KeyDown       += OnCaptureKeyDown;
            CaptureRoot.PointerPressed += (_, _) => CaptureRoot.Focus();

            // Sync label whenever the bound KeyValue changes (e.g. from ViewModel)
            PropertyChanged += (_, e) =>
            {
                if (e.Property == KeyValueProperty)
                    RefreshLabel();
            };

            RefreshLabel();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnCaptureGotFocus(object sender, GotFocusEventArgs e)
        {
            CaptureRoot.BorderBrush = AccentColour;

            KeyLabel.Text = KeyValue == 0
                ? "Press a key…"
                : $"{KeyDisplay}  ·  Press a key…";

            KeyLabel.Foreground = TextSecondaryColour;
        }

        private void OnCaptureLostFocus(object sender, RoutedEventArgs e)
        {
            CaptureRoot.BorderBrush = UnfocusedBorderColour;
            RefreshLabel();
        }

        private void OnCaptureKeyDown(object sender, KeyEventArgs e)
        {
            // Ignore standalone modifier presses — they are not useful hotkeys alone
            if (e.Key == Key.LeftShift   || e.Key == Key.RightShift  ||
                e.Key == Key.LeftCtrl    || e.Key == Key.RightCtrl   ||
                e.Key == Key.LeftAlt     || e.Key == Key.RightAlt    ||
                e.Key == Key.LWin        || e.Key == Key.RWin        ||
                e.Key == Key.None)
            {
                return;
            }

            KeyValue = (int)e.Key;
            RefreshLabel();
            e.Handled = true;

            // Surrender focus so the control doesn't swallow the next keystroke
            CaptureRoot.Focus(NavigationMethod.Tab);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshLabel()
        {
            if (KeyLabel == null) return;

            if (KeyValue == 0)
            {
                KeyLabel.Text       = "–";
                KeyLabel.Foreground = TextSecondaryColour;
            }
            else
            {
                KeyLabel.Text       = KeyDisplay;
                KeyLabel.Foreground = TextPrimaryColour;
            }
        }

        /// <summary>Converts a raw int key value to the display string.</summary>
        public static string KeyValueToDisplay(int keyValue)
        {
            if (keyValue == 0)
                return string.Empty;

            var key = (Key)keyValue;
            return key.ToString();
        }
    }
}
