namespace Core.Helpers
{
    /// <summary>
    /// Names are unique within a flow, and steps, areas and points share one namespace.
    /// </summary>
    public static class FlowNameHelper
    {
        /// <summary>
        /// The name if nothing has it, otherwise the same name with the first free number after it.
        /// "Find button" becomes "Find button 2", and the numbering starts at two because the first
        /// one was never called "Find button 1".
        /// </summary>
        public static string MakeUnique(string desired, IReadOnlyCollection<string> taken)
        {
            string name = desired.Trim();

            HashSet<string> used = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);
            if (!used.Contains(name))
                return name;

            // Bounded by how many names are already taken: the first number free of them all is
            // never further away than one past the last.
            for (int suffix = 2; suffix <= used.Count + 2; suffix++)
            {
                string candidate = $"{name} {suffix}";
                if (!used.Contains(candidate))
                    return candidate;
            }

            return name;
        }

        /// <summary>
        /// Names taken more than once, whatever their case, so the message can say which.
        /// Blank names are not duplicates of each other - a step with no name has its own rule.
        /// </summary>
        public static IReadOnlyList<string> Duplicates(IEnumerable<string> names)
        {
            return names
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
        }
    }
}
