using Avalonia.Input;
using InventoryKamera.Avalonia.Controls;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Tests for <see cref="KeyBindingCapture"/> that do not require a live Avalonia window.
    ///
    /// Interaction tests (focus + KeyDown event simulation) require Avalonia.Headless setup.
    /// Those are marked Skip and are scheduled for chunk 10.
    /// </summary>
    public class KeyBindingCaptureTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // KeyValueToDisplay — static helper, testable without Avalonia.Headless
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void KeyValueToDisplay_Returns_Empty_For_Zero()
        {
            var result = KeyBindingCapture.KeyValueToDisplay(0);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void KeyValueToDisplay_Returns_KeyName_For_W()
        {
            int wValue = (int)Key.W;
            var result = KeyBindingCapture.KeyValueToDisplay(wValue);
            Assert.Equal("W", result);
        }

        [Fact]
        public void KeyValueToDisplay_Returns_KeyName_For_B()
        {
            int bValue = (int)Key.B;
            var result = KeyBindingCapture.KeyValueToDisplay(bValue);
            Assert.Equal("B", result);
        }

        [Fact]
        public void KeyValueToDisplay_Returns_KeyName_For_ArrowKeys()
        {
            Assert.Equal("Up",    KeyBindingCapture.KeyValueToDisplay((int)Key.Up));
            Assert.Equal("Down",  KeyBindingCapture.KeyValueToDisplay((int)Key.Down));
            Assert.Equal("Left",  KeyBindingCapture.KeyValueToDisplay((int)Key.Left));
            Assert.Equal("Right", KeyBindingCapture.KeyValueToDisplay((int)Key.Right));
        }

        [Fact]
        public void KeyValueToDisplay_Returns_KeyName_For_FunctionKeys()
        {
            Assert.Equal("F1",  KeyBindingCapture.KeyValueToDisplay((int)Key.F1));
            Assert.Equal("F12", KeyBindingCapture.KeyValueToDisplay((int)Key.F12));
        }

        // ─────────────────────────────────────────────────────────────────────
        // KeyDown_Sets_KeyValue — requires Avalonia.Headless (skipped for now)
        // ─────────────────────────────────────────────────────────────────────

        [Fact(Skip = "Requires Avalonia.Headless (event infrastructure); scheduled for chunk 10")]
        public void KeyDown_Sets_KeyValue()
        {
            // To implement in chunk 10:
            //   var app = AvaloniaHeadlessApplicationBuilder.Create()...
            //   var control = new KeyBindingCapture();
            //   control.Focus();
            //   RaiseKeyDown(control, Key.W);
            //   Assert.Equal((int)Key.W, control.KeyValue);
        }
    }
}
