namespace InventoryKamera
{
    /// <summary>Paging rules for scrolling through the weapon and artifact grids.</summary>
    public static class InventoryPaging
    {
        /// <summary>
        /// True if the next page is the last one, the only page allowed to show fewer than a full
        /// grid of items. Every other page must detect a full grid; if detection comes up short it
        /// is retried (and as a last resort the previous page's grid positions are reused).
        /// </summary>
        /// <remarks>
        /// This used to compare the rows remaining with the number of <em>cards</em> per page
        /// (32 artifacts / 40 weapons), so with 822 artifacts (103 rows) every page after about
        /// page 19 accepted a short detection. A page that missed one row then skipped that row
        /// and scrolled one row too little, scanning the previous row twice.
        /// </remarks>
        public static bool IsLastPage(int totalRows, int rowsQueued, int rowsPerPage) =>
            totalRows - rowsQueued <= rowsPerPage;
    }
}
