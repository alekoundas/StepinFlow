using System.Reflection;

using Core.Models.Database;

using DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// A flow's rows, loaded to be compared across an import. The ids change, so rows are matched
    /// by name, and anything pointing at another row is compared by that row's name.
    /// </summary>
    public sealed class ScriptRows
    {
        // Ids and timestamps are the database's, not the flow's.
        private static readonly string[] NotFacts =
        [
            "Id", "CreatedOn", "UpdatedOn", "FlowId", "RootId", "ParentFlowAreaId", "FlowAreaId", "FlowStepId",
            "ParentFlowStepId", "FlowPointId", "FlowPointEndId", "FlowStepReferenceId", "FlowStepReferenceEndId", "SubFlowId",
        ];

        public List<FlowArea> Areas { get; init; } = [];
        public List<FlowPoint> Points { get; init; } = [];
        public List<FlowStep> Steps { get; init; } = [];
        public List<FlowStepTemplate> Templates { get; init; } = [];

        public static async Task<ScriptRows> LoadAsync(IDbContextFactory<AppDbContext> factory, int flowId)
        {
            await using AppDbContext db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
            CancellationToken ct = TestContext.Current.CancellationToken;

            return new ScriptRows
            {
                Areas = await db.FlowAreas.AsNoTracking().Where(x => x.FlowId == flowId).ToListAsync(ct),
                Points = await db.FlowPoints.AsNoTracking().Where(x => x.FlowId == flowId).ToListAsync(ct),
                Steps = await db.FlowSteps.AsNoTracking().Where(x => x.RootId == flowId).ToListAsync(ct),
                Templates = await db.FlowStepTemplates.AsNoTracking().Where(x => x.FlowStep.RootId == flowId).ToListAsync(ct),
            };
        }

        public string? AreaName(int? id)
        {
            return Areas.FirstOrDefault(x => x.Id == id)?.Name;
        }

        public string? StepName(int id)
        {
            return Steps.FirstOrDefault(x => x.Id == id)?.Name;
        }

        /// <summary>Every scalar the two rows disagree on, as "Field: before -> after".</summary>
        public static List<string> Differences(object before, object? after, params string[] alsoIgnored)
        {
            if (after == null)
                return ["missing"];

            List<string> differences = new List<string>();

            foreach (PropertyInfo property in before.GetType().GetProperties())
            {
                Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                bool isScalar = type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(byte[]);

                if (!isScalar || NotFacts.Contains(property.Name) || alsoIgnored.Contains(property.Name))
                    continue;

                object? left = property.GetValue(before);
                object? right = property.GetValue(after);

                bool same;
                if (left is byte[] leftBytes && right is byte[] rightBytes)
                    same = leftBytes.SequenceEqual(rightBytes);
                else
                    same = Equals(left, right);

                if (!same)
                    differences.Add($"{property.Name}: {left} -> {right}");
            }

            return differences;
        }
    }
}
