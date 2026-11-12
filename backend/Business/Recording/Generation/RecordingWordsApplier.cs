using Business.Recording.Draft;
using Business.Recording.Draft.Models;
using Business.Recording.Generation.Models;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Recording.Generation
{
    /// <summary>
    /// A model's words put on a recording's draft, field by field. A field that will not do - an
    /// empty name, one longer than a line, a check for words no screenshot showed after that click
    /// - keeps what the draft had and is said in NotTaken; the rest is taken. A name already used is
    /// numbered. So a mediocre answer still improves most of the flow, and nothing in it can stop
    /// the import or touch what was recorded: the words only ever land on steps that are already
    /// there, and a stage or a check only ever goes between two of them.
    /// </summary>
    public static class RecordingWordsApplier
    {
        private const int MINIMUM_NAME = 3;
        private const int MAXIMUM_NAME = 60;
        private const int MAXIMUM_STAGE_NAME = 40;
        private const int MAXIMUM_SENTENCE = 200;
        private const int MAXIMUM_NOTE = 300;

        // Under four letters, words on a screen are as often a misread icon as words.
        private const int MINIMUM_CHECK_LETTERS = 4;

        private static readonly char[] QUOTES = ['"', '\'', '`', '“', '”', '‘', '’'];

        /// <summary>A name, a comment and a failure message for each search the words name.</summary>
        public static void ApplyNames(RecordingDraft draft, RecordingOutline outline, RecordingWords words, AppliedWords applied)
        {
            foreach (StepWords step in words.Steps ?? [])
            {
                string id = step.Id?.Trim() ?? string.Empty;
                if (!outline.Searches.TryGetValue(id, out DraftItem? search))
                    continue;

                string name = Cleaned(step.Name, MAXIMUM_NAME + 1).TrimEnd('.');
                if (name.Length >= MINIMUM_NAME && name.Length <= MAXIMUM_NAME)
                    draft.Rename(search.Step, name);
                else if (name.Length > 0)
                    applied.NotTaken.Add($"The name for {id} was longer than a line, so it is still \"{search.Step.Name}\".");

                string comment = Cleaned(step.Comment, MAXIMUM_SENTENCE);
                if (comment.Length > 0)
                    search.Step.CodeComment = $"{comment}\n{search.Step.CodeComment}".TrimEnd();

                string notFound = Cleaned(step.NotFound, MAXIMUM_SENTENCE);
                if (notFound.Length > 0 && search.End != null)
                    search.End.Message = notFound;
            }

            TakeQuestionsAndNotes(words, applied);
        }

        /// <summary>
        /// A QA test's stages and checks. A stage goes before a search the tester began none at; a
        /// check only for words that appeared after that search's click, as a screen note showed
        /// them, so it can pass on the first execution.
        /// </summary>
        public static void ApplyTest(RecordingDraft draft, RecordingOutline outline, RecordingWords words, RecordingResultDto recording, AppliedWords applied)
        {
            foreach (StageWords stage in words.Stages ?? [])
            {
                string before = stage.Before?.Trim() ?? string.Empty;
                string name = Cleaned(stage.Name, MAXIMUM_STAGE_NAME + 1).TrimEnd('.');
                if (!outline.Searches.TryGetValue(before, out DraftItem? search) || name.Length == 0)
                    continue;

                if (name.Length > MAXIMUM_STAGE_NAME)
                {
                    applied.NotTaken.Add($"The stage before {before} had a name longer than a stage's, so it was left out.");
                    continue;
                }

                if (draft.AddStageBefore(search, name, Cleaned(stage.Comment, MAXIMUM_SENTENCE)) == null)
                    applied.NotTaken.Add($"A stage already begins at {before}, so \"{name}\" was left out.");
            }

            HashSet<string> checkedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CheckWords check in words.Checks ?? [])
            {
                string after = check.After?.Trim() ?? string.Empty;
                if (!outline.Searches.TryGetValue(after, out DraftItem? search))
                    continue;

                string checkWords = WordsOf(check.Words);
                if (checkWords.Count(char.IsLetter) < MINIMUM_CHECK_LETTERS || !checkedWords.Add(checkWords))
                    continue;

                List<string> appeared = recording.ScreenChanges
                    .Where(x => x.ActionIndex == search.ActionIndex)
                    .SelectMany(x => x.Appeared)
                    .ToList();

                if (!appeared.Any(x => x.Contains(checkWords, StringComparison.OrdinalIgnoreCase)))
                {
                    applied.NotTaken.Add($"A check for \"{checkWords}\" after {after} was left out: no screenshot showed those words appear after that click.");
                    continue;
                }

                string name = Cleaned(check.Name, MAXIMUM_NAME + 1).TrimEnd('.');
                if (name.Length < MINIMUM_NAME || name.Length > MAXIMUM_NAME)
                    name = RecordingDraft.NameFrom("Check", checkWords, "Check");

                draft.AddCheckAfter(search, checkWords, name, Cleaned(check.Comment, MAXIMUM_SENTENCE), Cleaned(check.NotFound, MAXIMUM_SENTENCE));
            }

            TakeQuestionsAndNotes(words, applied);
        }

        /// <summary>
        /// What makes a draft a QA test, whatever a model said: a stage, before the first search when
        /// no one named any, and an End Execution passed after everything recorded.
        /// </summary>
        public static void MakeTest(RecordingDraft draft, string stageName)
        {
            if (!draft.Items.Any(x => x.Kind == DraftItemKindEnum.STAGE))
            {
                DraftItem? first = draft.Items.FirstOrDefault(x => x.Kind == DraftItemKindEnum.SEARCH);
                if (first != null)
                    draft.AddStageBefore(first, Cleaned(stageName, MAXIMUM_STAGE_NAME), string.Empty);
            }

            bool isPassed = draft.Items.Any(x => x.Step.FlowStepType == FlowStepTypeEnum.END_EXECUTION && x.Step.EndExecutionAsSuccess);
            if (!isPassed)
                draft.AddPassed("everything the test looks for was seen");
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void TakeQuestionsAndNotes(RecordingWords words, AppliedWords applied)
        {
            foreach (string question in (words.Questions ?? []).Select(x => Cleaned(x, MAXIMUM_NOTE)).Where(x => x.Length > 0))
            {
                if (!applied.Questions.Contains(question, StringComparer.OrdinalIgnoreCase))
                    applied.Questions.Add(question);
            }

            foreach (string note in (words.Notes ?? []).Select(x => Cleaned(x, MAXIMUM_NOTE)).Where(x => x.Length > 0))
            {
                if (!applied.Notes.Contains(note, StringComparer.OrdinalIgnoreCase))
                    applied.Notes.Add(note);
            }
        }

        // On one line, without a quote's brackets or the quotes a small model puts round an answer,
        // and no longer than it may be.
        private static string Cleaned(string? text, int maximum)
        {
            string cleaned = RecordingDraft.Quotable(text ?? string.Empty).Trim(QUOTES).Trim();
            if (cleaned.Length > maximum)
                cleaned = cleaned[..maximum].TrimEnd();

            return cleaned;
        }

        // As a note shows them, without the quotes round them or the ellipsis that ends a line cut short.
        private static string WordsOf(string? text)
        {
            string words = RecordingDraft.Quotable(text ?? string.Empty).Trim(QUOTES).Trim();
            words = words.TrimEnd('…').TrimEnd('.').Trim().Trim(QUOTES).Trim();

            return words;
        }
    }
}
