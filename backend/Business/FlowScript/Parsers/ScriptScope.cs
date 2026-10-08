using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Parsers
{
    /// <summary>
    /// Store the related data defined in the script header. (Steps, areas, points and csvColumns)
    ///
    /// A name nothing above declares is reported at the token it was read from.
    /// </summary>
    internal sealed class ScriptScope
    {
        private readonly List<Diagnostic> _diagnostics;

        private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Steps, areas, points and csvColumns share one set of names.
        private readonly Dictionary<string, FlowArea> _areas = new Dictionary<string, FlowArea>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowPoint> _points = new Dictionary<string, FlowPoint>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowStep> _steps = new Dictionary<string, FlowStep>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowStepTemplate> _templates = new Dictionary<string, FlowStepTemplate>(StringComparer.OrdinalIgnoreCase);

        public ScriptScope(List<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
        }


        // ================================================================
        // Public methods - declaring
        // ================================================================

        public void Declare(FlowArea area, ScriptToken at)
        {
            bool isDeclared = DeclareName(area.Name, at);
            if (isDeclared)
                _areas[area.Name] = area;
        }

        public void Declare(FlowPoint point, ScriptToken at)
        {
            bool isDeclared = DeclareName(point.Name, at);
            if (isDeclared)
                _points[point.Name] = point;
        }

        public void Declare(FlowStep step, ScriptToken at)
        {
            bool isDeclared = DeclareName(step.Name, at);
            if (isDeclared)
                _steps[step.Name] = step;
        }

        public void Declare(FlowCsvColumn input, ScriptToken at)
        {
            DeclareName(input.Name, at);
        }

        public void Declare(FlowStepTemplate template, ScriptToken at)
        {
            if (!_templates.TryAdd(template.Name, template))
                Report(DiagnosticCodeEnum.TEMPLATE_DUPLICATE, at, $"\"{template.Name}\" is already described above.");
        }


        // ================================================================
        // Public methods - resolving
        // ================================================================

        public FlowArea? Area(string name, ScriptToken at)
        {
            if (_areas.TryGetValue(name, out FlowArea? row))
                return row;

            Report(DiagnosticCodeEnum.NAME_UNKNOWN, at, $"Nothing above this line is called \"{name}\", so there is no area to point at.");

            return null;
        }

        public FlowPoint? Point(string name, ScriptToken at)
        {
            if (_points.TryGetValue(name, out FlowPoint? row))
                return row;

            Report(DiagnosticCodeEnum.NAME_UNKNOWN, at, $"Nothing above this line is called \"{name}\", so there is no point to point at.");

            return null;
        }

        public FlowStep? Step(string name, ScriptToken at)
        {
            if (_steps.TryGetValue(name, out FlowStep? row))
                return row;

            Report(DiagnosticCodeEnum.NAME_UNKNOWN, at, $"Nothing above this line is called \"{name}\", so there is no step to point at.");

            return null;
        }

        public FlowStepTemplate? Template(string fileName, ScriptToken at)
        {
            if (_templates.TryGetValue(fileName, out FlowStepTemplate? row))
                return row;

            Report(DiagnosticCodeEnum.TEMPLATE_UNKNOWN, at, $"\"{fileName}\" has no line under Templates above, so nothing says where it is clicked.");

            return null;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // False when the name is taken, which is reported at the second one.
        private bool DeclareName(string name, ScriptToken at)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_names.Add(name))
                return true;

            Report(DiagnosticCodeEnum.NAME_DUPLICATE, at,
                $"\"{name}\" is already used above. Steps, areas, points and inputs share one set of names, because the script refers to them by name.");

            return false;
        }


        private void Report(DiagnosticCodeEnum code, ScriptToken at, string message)
        {
            _diagnostics.Add(Diagnostic.Error(code, at.Line, at.Column, message));
        }
    }
}
