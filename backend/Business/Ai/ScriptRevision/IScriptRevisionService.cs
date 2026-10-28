using Business.Ai.ScriptRevision.Models;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;

namespace Business.Ai.ScriptRevision
{
    public interface IScriptRevisionService
    {
        /// <summary>
        /// A script and an instruction in, a script that reads cleanly out. The model's answer is
        /// read back the way an import reads a file and checked against what it may not change,
        /// and what it got wrong is sent back to it, ATTEMPT_COUNT attempts at most. checkAnswer is
        /// what this caller requires of an answer that reads cleanly, beyond what every answer must
        /// keep. onAttempt is told each attempt's number, and why the one before it was refused.
        /// </summary>
        Task<ScriptRevisionResult> ReviseAsync(
            string script,
            string instruction,
            Func<FlowScriptSchema, IReadOnlyList<Diagnostic>>? checkAnswer,
            Action<int, IReadOnlyList<Diagnostic>>? onAttempt,
            CancellationToken ct = default);
    }
}
