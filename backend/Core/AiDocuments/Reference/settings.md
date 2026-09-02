# Settings

## Recording

**Capture width** and **Capture height** — how much of the screen is captured around the pointer
each time you click while recording. Bigger gives the wizard more to crop a template from.

## Execution

**Screenshots kept before a failure** — how many frames of run-up are written out with a failed
step. Nothing is written while a flow succeeds.

**Screenshots kept per run** — how many screenshots a run leaves behind so you can see what a step
was looking at. Zero keeps none.

## AI

**AI provider** — `NONE`, `OLLAMA` or `OPENAI`. Nothing AI-related is available until one is
chosen; every AI feature checks first and stays disabled rather than failing when clicked.

**Ollama address** — where Ollama is listening. The default is right unless you moved it.

**API key** — needed only for a provider that charges. Once saved it is never sent back to the
page; the page is told a key is set, not what it is.

**Model** — which model to ask. For Ollama this is the list of models you have downloaded; for
OpenAI a short curated list.

**Send text read from the screen** — on for Ollama, off and unchangeable for anything else. Text a
Read Text step found could be an account number or a password, so it is never sent to a provider
outside this machine. The rule is the provider, not a checkbox.

## OCR languages

Lists the language packs installed and the ones available to install. Installing uses the Windows
language installer and asks for elevation.

## Hotkeys

Debugger keys — continue, step into, step over, pause, stop. Defaults are single keys rather than
combinations.

## Discord bots

Name, webhook URL, the bot name to post as, an avatar, and a rate limit in seconds. The webhook URL
is the credential and is never logged.
