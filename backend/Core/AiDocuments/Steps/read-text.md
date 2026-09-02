# Read Text step

## What Read Text does

`READ_TEXT` reads text inside an area using Windows OCR. It has **Success** and **Failure**
branches, and what it read is available to steps below it.

It works off the raw screen capture with no image encoding in between.

## Read Text modes

Three of the four search modes are offered:

| Mode | What it does |
|---|---|
| Read once | Reads the area once. Succeeds if anything was read. |
| Until it matches | Reads repeatedly until the condition holds. |
| Until it stops matching | Reads repeatedly until the condition stops holding. |

`FIND_ALL` is not offered. Reading gives one block of text and no positions, so there is nothing to
act on one at a time.

**Read once** has no condition fields at all. It succeeds if anything was read — whitespace is
trimmed first, and a blank area reads as a newline.

## OCR languages

The language is a dropdown of the language packs Windows actually has installed, so you cannot pick
one Windows cannot read. English works out of the box.

More languages can be installed from Settings, which uses the Windows language pack installer.

## Keep only — narrowing before testing

**Keep only** is a regular expression applied to what was read, keeping the first capture group.
It runs **before** the condition is checked.

That ordering lets one step do both jobs: keep `(\d+)%` and then succeed if the result is not
empty, and you have extracted a percentage and checked it was there in a single step.

If the expression does not match, the result is empty.

## Conditions

The waiting modes succeed when the condition holds. Conditions are: contains, is exactly, and
matches a pattern.

## Testing a Read Text

The **Test** button shows everything that was read **and** what survived the Keep only expression —
not just whether it passed.

This is what diagnoses most OCR problems. Seeing `Loqin` where you expected `Login` tells you
immediately that the text is being misread and the condition is fine.
