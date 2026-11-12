using System.Threading.Tasks;
using InventoryKamera;
using InventoryKamera.Configuration;
using InventoryKamera.Infrastructure;
using Moq;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class InventoryScannerTests
    {
        /// <summary>
        /// A worker that reaches the END marker (after the last queued artifact) stops the other
        /// workers, but must not mark the scan as cancelled: the scan checks that flag between
        /// categories, and once characters moved to the end, a full scan stopped right after
        /// Character Development Items, without materials or characters.
        /// </summary>
        [Fact]
        public void Worker_ReachingEnd_DoesNotCancelTheScan()
        {
            var scanner = new InventoryScanner(
                Mock.Of<IScreenCapture>(), Mock.Of<IInputSimulator>(), Mock.Of<IOcrEngine>(),
                Mock.Of<IImageProcessor>(), Mock.Of<IUserInterface>(), Mock.Of<IGameDataService>(),
                Mock.Of<IScanProfileService>(), new ScanSettings());
            InventoryScanner.workerQueue.Enqueue(new OCRImageCollection(null, "END", 0));

            var worker = Task.Run(scanner.ImageProcessorWorker);

            Assert.True(worker.Wait(5000), "The worker should stop after END");
            Assert.False(scanner.WasCancelled);
        }
    }
}
