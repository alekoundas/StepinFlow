using System.Security.Cryptography;
using System.Text;

using Core.Models.Database;

namespace Core.Helpers
{
    /// <summary>
    /// The shape of a flow: which steps exist, where they sit and in what order.
    ///
    /// Blind to names and settings on purpose. Renaming a step or changing a template alters what a
    /// step does without altering which steps ran in what order, and a run's history stays valid
    /// through those.
    /// </summary>
    public static class FlowStructureHasher
    {
        public static string Hash(IEnumerable<FlowStep> steps)
        {
            StringBuilder builder = new StringBuilder();

            foreach (FlowStep step in steps.OrderBy(x => x.Id))
            {
                builder.Append(step.Id)
                    .Append(':')
                    .Append(step.ParentFlowStepId)
                    .Append(':')
                    .Append(step.OrderNumber)
                    .Append(';');
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
            return Convert.ToHexString(hash);
        }
    }
}
