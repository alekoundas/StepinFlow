namespace Core.Models.Business
{
    /// <summary>
    /// Which hit of how many a FIND_ALL search still has to work through.
    ///
    /// Not a mouse cursor, and not named one: "cursor" means the mouse everywhere else in here.
    /// </summary>
    public class PendingMatches
    {
        public PendingMatches(int index, int count)
        {
            Index = index;
            Count = count;
        }

        public int Index { get; }
        public int Count { get; }

        public bool HasNext => Index + 1 < Count;


        // ================================================================
        // Public methods
        // ================================================================

        public PendingMatches Advance()
        {
            return new PendingMatches(Index + 1, Count);
        }
    }
}
