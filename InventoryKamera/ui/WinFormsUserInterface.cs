using System.Drawing;
using System.Windows.Forms;
using InventoryKamera;

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

        public void SetGear(Bitmap bm, Weapon weapon)
        {
            var clone = (Bitmap)bm.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetGear(clone, weapon); clone.Dispose(); }));
        }

        public void SetGear(Bitmap bm, Artifact artifact)
        {
            var clone = (Bitmap)bm.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetGear(clone, artifact); clone.Dispose(); }));
        }

        public void SetGearPictureBox(Bitmap bm)
        {
            var clone = (Bitmap)bm.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetGearPictureBox(clone); clone.Dispose(); }));
        }

        public void SetGearTextBox(string text)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetGearTextBox(text)));
        }

        // Character

        public void SetCharacter_NameAndElement(Bitmap bm, string name, string element)
        {
            var clone = (Bitmap)bm.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetCharacter_NameAndElement(clone, name, element); clone.Dispose(); }));
        }

        public void SetCharacter_Level(Bitmap bm, int level, int maxLevel)
        {
            var clone = (Bitmap)bm.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetCharacter_Level(clone, level, maxLevel); clone.Dispose(); }));
        }

        public void SetCharacter_Constellation(int level)
        {
            _form.BeginInvoke((MethodInvoker)(() => UserInterface.SetCharacter_Constellation(level)));
        }

        public void SetCharacter_Talent(Bitmap bm, string text, int i)
        {
            var clone = (Bitmap)bm.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetCharacter_Talent(clone, text, i); clone.Dispose(); }));
        }

        // Materials / Mora

        public void SetMora(Bitmap mora, int count)
        {
            var clone = (Bitmap)mora.Clone();
            _form.BeginInvoke((MethodInvoker)(() => { UserInterface.SetMora(clone, count); clone.Dispose(); }));
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

        public void SetNavigation_Image(Bitmap bm)
        {
            var clone = (Bitmap)bm.Clone();
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
    }
}
