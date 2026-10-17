import { HighlightStyle, StreamLanguage, foldService, syntaxHighlighting } from "@codemirror/language";
import type { StreamParser } from "@codemirror/language";
import type { Extension } from "@codemirror/state";
import { tags } from "@lezer/highlight";

// The words of the flow format, as Business/FlowScript/Catalogs/ScriptKeywordCatalog spells them.
const HEADERS = ["Flow:", "Id:", "Sizes:", "Description:", "Areas:", "Points:", "Inputs:", "Templates:", "Steps:"];

const STEPS = [
  "Wait Until No Image",
  "Wait Until No Text",
  "Find All Images",
  "Wait For Image",
  "Wait For Text",
  "End Execution",
  "Resize Window",
  "Focus Window",
  "Move Window",
  "Check Value",
  "Find Image",
  "Check Text",
  "Sub Flow",
  "Go Back",
  "Notify",
  "Scroll",
  "System",
  "Launch",
  "Click",
  "Press",
  "Drag",
  "Move",
  "Loop",
  "Type",
  "Wait",
  "Run",
];

const OPTIONS = [
  "main",
  "inside",
  "on screen",
  "ratio",
  "offset",
  "at",
  "window",
  "monitor",
  "primary",
  "process",
  "title",
  "size",
  "scales with",
  "default",
  "secret",
  "click",
  "captured",
  "to",
  "point",
  "nowhere",
  "match",
  "template",
  "accuracy",
  "required",
  "in",
  "opens",
  "keep",
  "timeout",
  "no timeout",
  "and",
  "times",
  "forever",
  "passed",
  "failed",
  "is not empty",
  "is empty",
  "is not",
  "is",
  "does not contain",
  "contains",
  "matches",
  "between",
  "starts with",
  "shape and brightness",
  "shape",
  "right",
  "middle",
  "double",
  "hold",
  "release",
  "up",
  "down",
  "left",
  "dpi",
  "area",
];

const STEP_PATTERN = wordsPattern(STEPS);
const OPTION_PATTERN = wordsPattern(OPTIONS);
const HEADER_PATTERN = new RegExp(`^(?:${HEADERS.map(escape).join("|")})`);
const NUMBER_PATTERN = /^-?\d+(?:\.\d+)?(?:x\d+|ms|dpi)?(?![\w.])/;
const SECTION_PATTERN = /^##(?:\s|$)/;

interface ScriptState {
  isInQuote: boolean;
  isRestOfLine: boolean;
}

/**
 * Colours only. A line is read the way the scanner reads one - a step's keyword where the line
 * starts, then its options, its <[ quoted ]> text and its numbers - but nothing here says whether a
 * script is right: Scanner.Read is the one judge, and what it says arrives as lint marks.
 */
const PARSER: StreamParser<ScriptState> = {
  name: "flowscript",
  startState: () => ({ isInQuote: false, isRestOfLine: false }),
  token(stream, state) {
    // Quotes and a header's value end with their line.
    if (stream.sol()) {
      state.isInQuote = false;
      state.isRestOfLine = false;
    }

    if (state.isRestOfLine) {
      stream.skipToEnd();
      return "string";
    }

    if (state.isInQuote) return quoted(stream, state);

    if (stream.eatSpace()) return null;

    if (stream.column() === stream.indentation()) {
      if (stream.match(SECTION_PATTERN)) {
        stream.skipToEnd();
        return "heading";
      }

      if (stream.peek() === "#") {
        stream.skipToEnd();
        return "comment";
      }

      if (stream.match(HEADER_PATTERN)) {
        state.isRestOfLine = stream.current() === "Flow:" || stream.current() === "Id:";
        return "heading";
      }

      if (stream.match("Success:")) return "branchSuccess";
      if (stream.match("Failure:")) return "branchFailure";
      if (stream.match(STEP_PATTERN)) return "keyword";
    }

    if (stream.match("<[")) {
      state.isInQuote = true;
      return "bracket";
    }

    if (stream.match(NUMBER_PATTERN)) return "number";
    if (stream.match(OPTION_PATTERN)) return "atom";
    if (stream.match(/^[<>=!]+/)) return "operator";

    if (!stream.match(/^[^\s<]+/)) stream.next();
    return null;
  },
  tokenTable: {
    branchSuccess: tags.labelName,
    branchFailure: tags.special(tags.labelName),
  },
};

// The theme's own variables, so the script follows the app's colours.
const HIGHLIGHT = HighlightStyle.define([
  { tag: tags.heading, color: "var(--primary-color)", fontWeight: "600" },
  { tag: tags.comment, color: "var(--text-color-secondary)", fontStyle: "italic" },
  { tag: tags.keyword, color: "var(--purple-200)", fontWeight: "600" },
  { tag: tags.atom, color: "var(--indigo-300)" },
  { tag: tags.number, color: "var(--yellow-400)" },
  { tag: tags.string, color: "var(--text-color)" },
  { tag: tags.bracket, color: "var(--surface-300)" },
  { tag: tags.variableName, color: "var(--green-400)" },
  { tag: tags.operator, color: "var(--text-color)" },
  { tag: tags.labelName, color: "var(--green-400)", fontWeight: "600" },
  { tag: tags.special(tags.labelName), color: "var(--red-300)", fontWeight: "600" },
]);

/**
 * A line folds what is indented under it - a step its branches, a branch its steps, a header its
 * lines - and a ## section everything up to the next one, since a section does not indent the
 * steps under it.
 */
const FOLDING = foldService.of((state, lineStart, lineEnd) => {
  const line = state.doc.lineAt(lineStart);
  if (line.text.trim() === "") return null;

  const isSection = SECTION_PATTERN.test(line.text);
  const indent = indentOf(line.text);
  let last = line;

  for (let number = line.number + 1; number <= state.doc.lines; number++) {
    const next = state.doc.line(number);
    if (next.text.trim() === "") continue;

    if (isSection && SECTION_PATTERN.test(next.text)) break;
    if (!isSection && indentOf(next.text) <= indent) break;

    last = next;
  }

  if (last.number === line.number) return null;

  return { from: lineEnd, to: last.to };
});

export function flowScript(): Extension {
  return [StreamLanguage.define(PARSER), syntaxHighlighting(HIGHLIGHT), FOLDING];
}

function quoted(stream: Parameters<StreamParser<ScriptState>["token"]>[0], state: ScriptState): string {
  if (stream.match("]>")) {
    state.isInQuote = false;
    return "bracket";
  }

  if (stream.match(/^\{\{[^}]*\}\}/)) return "variableName";
  if (stream.match(/^(?:(?!\]>|\{\{).)+/)) return "string";

  stream.next();
  return "string";
}

function wordsPattern(words: string[]): RegExp {
  const longestFirst = [...words].sort((a, b) => b.length - a.length).map(escape);
  return new RegExp(`^(?:${longestFirst.join("|")})(?![\\w])`);
}

function escape(word: string): string {
  return word.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function indentOf(text: string): number {
  return text.length - text.trimStart().length;
}
