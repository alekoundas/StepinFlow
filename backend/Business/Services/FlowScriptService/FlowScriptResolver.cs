using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
    /// <summary>
    /// A parsed document with every name turned into an id.
    ///
    /// Separate from the reader because it needs the whole file: a cursor step can aim at a check
    /// written below it, so nothing can be resolved until everything has been read. Separate from
    /// the importer because it needs no database - the ids it hands out are positions in the
    /// document, and the importer swaps them for real ones when the rows are written.
    ///
    /// That also makes the round trip testable on its own: write, read, resolve, write, compare.
    /// </summary>
    public static class FlowScriptResolver
    {
        public static FlowScriptSource Resolve(FlowScriptDocument document, IReadOnlyList<FlowScriptError> errors)
        {
            List<FlowScriptError> problems = (List<FlowScriptError>)errors;

            Flow flow = new Flow { Id = 1, Name = document.FlowName, PublicId = document.PublicId };

            Dictionary<string, int> areaIds = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> pointIds = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> stepIds = new Dictionary<string, int>(StringComparer.Ordinal);

            int next = 1;

            foreach (ParsedArea parsed in document.Areas)
            {
                parsed.Area.Id = next++;
                parsed.Area.FlowId = flow.Id;
                areaIds[parsed.Area.Name] = parsed.Area.Id;
            }

            foreach (ParsedPoint parsed in document.Points)
            {
                parsed.Point.Id = next++;
                parsed.Point.FlowId = flow.Id;
                pointIds[parsed.Point.Name] = parsed.Point.Id;
            }

            foreach (ParsedStep parsed in document.Steps)
            {
                parsed.Step.Id = next++;
                parsed.Step.RootId = flow.Id;

                // A branch row has no name, and two of them under different steps are not a clash.
                if (!string.IsNullOrEmpty(parsed.Step.Name))
                    stepIds[parsed.Step.Name] = parsed.Step.Id;
            }

            // Second pass: everything written as a name becomes the id it names.
            foreach (ParsedArea parsed in document.Areas)
            {
                if (parsed.ParentName == null)
                    continue;

                parsed.Area.ParentFlowAreaId = Lookup(areaIds, parsed.ParentName, parsed.Line, "area", problems);
            }

            foreach (ParsedPoint parsed in document.Points)
            {
                if (parsed.AreaName == null)
                    continue;

                parsed.Point.FlowAreaId = Lookup(areaIds, parsed.AreaName, parsed.Line, "area", problems);
            }

            for (int i = 0; i < document.Steps.Count; i++)
            {
                ParsedStep parsed = document.Steps[i];
                FlowStep step = parsed.Step;

                step.FlowId = parsed.ParentIndex == null ? flow.Id : null;
                step.ParentFlowStepId = parsed.ParentIndex == null ? null : document.Steps[parsed.ParentIndex.Value].Step.Id;

                if (parsed.AreaName != null)
                    step.FlowAreaId = Lookup(areaIds, parsed.AreaName, parsed.Line, "area", problems);

                if (parsed.PointName != null)
                    step.FlowPointId = Lookup(pointIds, parsed.PointName, parsed.Line, "point", problems);

                if (parsed.PointEndName != null)
                    step.FlowPointEndId = Lookup(pointIds, parsed.PointEndName, parsed.Line, "point", problems);

                if (parsed.ReferenceName != null)
                    step.FlowStepReferenceId = Lookup(stepIds, parsed.ReferenceName, parsed.Line, "step", problems);

                if (parsed.ReferenceEndName != null)
                    step.FlowStepReferenceEndId = Lookup(stepIds, parsed.ReferenceEndName, parsed.Line, "step", problems);
            }

            return Build(document, flow);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static int? Lookup(
            IReadOnlyDictionary<string, int> known, string name, int line, string what, List<FlowScriptError> problems)
        {
            if (known.TryGetValue(name, out int id))
                return id;

            problems.Add(new FlowScriptError(line, 1, $"Nothing in this flow is called \"{name}\", so there is no {what} to point at."));

            return null;
        }

        private static FlowScriptSource Build(FlowScriptDocument document, Flow flow)
        {
            List<FlowStep> steps = document.Steps.Select(x => x.Step).ToList();

            Dictionary<int, IReadOnlyList<string>> templates = document.Steps
                .Where(x => x.TemplateFileNames.Count > 0)
                .ToDictionary(x => x.Step.Id, x => (IReadOnlyList<string>)x.TemplateFileNames);

            // A sub-flow is named by the path it lives at; the importer is what finds the flow.
            Dictionary<int, string> subFlowPaths = new Dictionary<int, string>();
            foreach (ParsedStep parsed in document.Steps.Where(x => x.SubFlowPath != null))
            {
                parsed.Step.SubFlowId = parsed.Step.Id;
                subFlowPaths[parsed.Step.Id] = parsed.SubFlowPath!;
            }

            return new FlowScriptSource
            {
                Flow = flow,
                Areas = document.Areas.Select(x => x.Area).ToList(),
                Points = document.Points.Select(x => x.Point).ToList(),
                Inputs = document.Inputs,
                Viewports = document.Viewports,
                Steps = steps,
                AreaNamesById = document.Areas.ToDictionary(x => x.Area.Id, x => x.Area.Name),
                PointNamesById = document.Points.ToDictionary(x => x.Point.Id, x => x.Point.Name),
                StepNamesById = steps.Where(x => !string.IsNullOrEmpty(x.Name)).ToDictionary(x => x.Id, x => x.Name),
                TemplateFileNamesByStepId = templates,
                SubFlowPathsById = subFlowPaths,
            };
        }
    }
}
