using InventoryKamera;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class InventoryPagingTests
    {
        /// <summary>
        /// 822 artifacts = 103 rows of 8, 4 rows per page. Before the fix, page 21 (80 rows queued)
        /// counted as the last page because 23 rows remaining &lt;= 32 cards per page, so a detection
        /// that missed a row was accepted: that row was skipped and the previous one scanned twice.
        /// </summary>
        [Theory]
        [InlineData(103, 80, 4, false)] // page 21 of the failing scan: not the last page
        [InlineData(103, 72, 4, false)]
        [InlineData(103, 99, 4, true)]  // 4 rows left: last page
        [InlineData(103, 100, 4, true)] // 3 rows left: last page, may be short
        [InlineData(20, 15, 5, true)]   // weapons: 5 rows per page
        [InlineData(20, 10, 5, false)]
        public void IsLastPage_ComparesRowsWithRows(int totalRows, int rowsQueued, int rowsPerPage, bool expected)
        {
            Assert.Equal(expected, InventoryPaging.IsLastPage(totalRows, rowsQueued, rowsPerPage));
        }
    }
}
