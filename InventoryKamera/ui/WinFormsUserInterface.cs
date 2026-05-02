using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera.UI
{
    internal class WinFormsUserInterface : IUserInterface
    {
        private readonly MainForm _form;

        public WinFormsUserInterface(MainForm form)
        {
            _form = form;
        }

        // Gear (weapons and artifacts)

        public void SetGear(Image<Rgba32> bm, Weapon weapon)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetGear(clone, weapon); clone.Dispose(); }));
        }

        public void SetGear(Image<Rgba32> bm, Artifact artifact)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetGear(clone, artifact); clone.Dispose(); }));
        }

        public void SetGearPictureBox(Image<Rgba32> bm)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetGearPictureBox(clone); clone.Dispose(); }));
        }

        public void SetGearTextBox(string text)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetGearTextBox(text)));
        }

        // Character

        public void SetCharacter_NameAndElement(Image<Rgba32> bm, string name, string element)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetCharacter_NameAndElement(clone, name, element); clone.Dispose(); }));
        }

        public void SetCharacter_Level(Image<Rgba32> bm, int level, int maxLevel)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetCharacter_Level(clone, level, maxLevel); clone.Dispose(); }));
        }

        public void SetCharacter_Constellation(int level)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetCharacter_Constellation(level)));
        }

        public void SetCharacter_Talent(Image<Rgba32> bm, string text, int i)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetCharacter_Talent(clone, text, i); clone.Dispose(); }));
        }

        // Materials / Mora

        public void SetMora(Image<Rgba32> mora, int count)
        {
            var clone = ImageSharpToBitmap(mora);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetMora(clone, count); clone.Dispose(); }));
        }

        public void SetMaterial(Image<Rgba32> bitmap, string name, int count)
        {
            // The legacy WinForms SetMaterial took both nameplate and quantity bitmaps;
            // we only have one image to surface, so we pass it as the nameplate slot and
            // let the count display in the text box.
            var clone = bitmap == null ? null : ImageSharpToBitmap(bitmap);
            _form.BeginInvoke((MethodInvoker)(() =>
            {
                UserInterface.SetMaterial(clone, null, name, count);
                clone?.Dispose();
            }));
        }

        // Counters / Max

        public void SetWeapon_Max(int value)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetWeapon_Max(value)));
        }

        public void SetArtifact_Max(int value)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetArtifact_Max(value)));
        }

        public void IncrementWeaponCount()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.IncrementWeaponCount()));
        }

        public void IncrementArtifactCount()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.IncrementArtifactCount()));
        }

        public void IncrementCharacterCount()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.IncrementCharacterCount()));
        }

        // Status and errors

        public void SetProgramStatus(string status, bool ok = true)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetProgramStatus(status, ok)));
        }

        public void AddError(string error)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.AddError(error)));
        }

        public void SetMainCharacterName(string name)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetMainCharacterName(name)));
        }

        // Navigation image

        public void SetNavigation_Image(Image<Rgba32> bm)
        {
            var clone = ImageSharpToBitmap(bm);
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetNavigation_Image(clone); clone.Dispose(); }));
        }

        // Reset methods

        public void ResetGearDisplay()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.ResetGearDisplay()));
        }

        public void ResetCharacterDisplay()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.ResetCharacterDisplay()));
        }

        public void ResetCounters()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.ResetCounters()));
        }

        public void ResetErrors()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.ResetErrors()));
        }

        public void ResetAll()
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.ResetAll()));
        }

        /// <summary>
        /// Converts an ImageSharp <see cref="Image{Rgba32}"/> to a GDI+ <see cref="Bitmap"/> for display in PictureBox controls.
        /// </summary>
        internal static Bitmap ImageSharpToBitmap(Image<Rgba32> image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, new PngEncoder());
                ms.Position = 0;
                return new Bitmap(ms);
            }
        }
    }
}
