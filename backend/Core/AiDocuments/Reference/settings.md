# Settings

## Execution

**Screenshots kept before a failure** — how many screenshots of the lead-up are written out with a failed
step. Nothing is written while a flow succeeds.

**Screenshots kept per execution** — how many screenshots an execution leaves behind so you can see what a step
was looking at. Zero keeps none.

## Repository

Where every flow is kept as a file, in a git repository on this computer, with **Open folder**, the
branch, the number of commits and how many flows have changes not committed yet.

**Name** and **Email** — who every commit is by. Left empty, they come from the computer's git
configuration, shown in the empty field; with neither, the commit is by the computer's user name.

## AI

Every AI feature runs on a model on this machine, through Ollama, and is off without it: install
Ollama from ollama.com, start it, and choose a model it has downloaded. Each feature checks first
and stays disabled rather than failing when clicked, and so does "This flow is a QA test" when
recording. The top of the panel says whether AI is on, and if not, what is in the way.

**Ollama address** — where Ollama is listening. The default is right unless you moved it.

**Model** — which of the models downloaded into Ollama to use. One can be downloaded from the list
under it.

**Cloud model** — `NONE` or `OPENAI`, on top of the local model and never instead of it. Once it has
a key and a model it answers in the local model's place.

**API key** — the cloud model's. Once saved it is never sent back to the page; the page is told a
key is set, not what it is.

**Model**, under the cloud model — a short curated list.

**Let the cloud model see your screen** — off by default. Text a Read Text step found could be an
account number or a password, and a screenshot is whatever was on screen. The local model always
sees it, because nothing leaves this machine.

## OCR languages

Lists the language packs installed and the ones available to install. Installing uses the Windows
language installer and asks for elevation.

## Hotkeys

Recording keys — pause or resume, and stop, F9 and F10 by default. They work whatever window is in
front and are never recorded, but still reach that window, so rebind any the recorded application
uses.

Debugger keys — continue, step into, step over, pause, stop. Defaults are single keys rather than
combinations.

## Discord bots

Name, webhook URL, the bot name to post as, an avatar, and a rate limit in seconds. The webhook URL
is the credential and is never logged.
