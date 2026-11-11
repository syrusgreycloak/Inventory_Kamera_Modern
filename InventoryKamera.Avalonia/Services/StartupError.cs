using System;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>What to tell the user when the app can't start (shown in a message box by Program).</summary>
    public static class StartupError
    {
        /// <summary>The innermost error's message, with a hint for known causes.</summary>
        /// <remarks>
        /// The OCR engines are created at startup. Without the Visual C++ runtime, loading
        /// leptonica fails with a DllNotFoundException ("Failed to find library"), even though the
        /// DLL is there: it's its dependencies that are missing. Releases ship the runtime DLLs next
        /// to the exe; this hint covers builds that don't.
        /// </remarks>
        public static string Message(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;

            string hint = inner is DllNotFoundException
                ? "A library the OCR engine needs couldn't be loaded. Installing the Microsoft Visual C++ " +
                  "Redistributable (x64) usually fixes this: https://aka.ms/vs/17/release/vc_redist.x64.exe"
                : "Please report this on GitHub with the log file attached.";

            return "Inventory Kamera Modern couldn't start.\n\n" +
                   inner.Message + "\n\n" + hint + "\n\n" +
                   @"Details: logging\InventoryKamera.Avalonia.log next to the program.";
        }
    }
}
