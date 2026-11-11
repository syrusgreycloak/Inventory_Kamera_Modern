using System;
using System.Reflection;
using InventoryKamera.Avalonia.Services;
using Xunit;

namespace InventoryKamera.Tests
{
    public class StartupErrorTests
    {
        [Fact]
        public void Message_MissingNativeLibrary_SuggestsVcRedist()
        {
            // As seen on a clean Windows: OCR engine creation wrapped by reflection and DI
            var ex = new TargetInvocationException(
                new DllNotFoundException("Failed to find library 'leptonica-1.85.0.dll' for platform x64"));

            string message = StartupError.Message(ex);

            Assert.Contains("leptonica-1.85.0.dll", message);
            Assert.Contains("vc_redist.x64.exe", message);
        }

        [Fact]
        public void Message_OtherError_AsksForReport()
        {
            string message = StartupError.Message(new InvalidOperationException("Something else"));

            Assert.Contains("Something else", message);
            Assert.Contains("report", message);
            Assert.DoesNotContain("vc_redist", message);
        }
    }
}
