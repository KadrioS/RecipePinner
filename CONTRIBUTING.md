# Contributing

Thanks for taking the time. Translations are the contribution this mod benefits
from most, and they are the easiest to get started with.

## Translations

Language files live in [`src/RecipePinner/Localization/`](src/RecipePinner/Localization/),
one JSON file per language. The file name has to match Valheim's own name for
that language, for example `Turkish.json` or `Portuguese_Brazilian.json`.

[`English.json`](src/RecipePinner/Localization/English.json) is the reference.
To fix or add a translation, copy a value across and change the text, not the key.

### The three rules that actually matter

**1. Your file must contain exactly the same keys as `English.json`.**
A missing key does not crash anything - the mod quietly falls back to English for
that one string. That is the problem: a half-translated file looks fine and ships
without anyone noticing. Add every key, even if you leave some values in English
for now.

**2. Keep one `"key": "value",` pair per line.**
The mod does not use a JSON library. It reads the file line by line and splits
each line at the first `:`. That means a file which is perfectly valid JSON can
still load almost nothing if it is formatted differently. In particular:

- Do not minify the file.
- Do not split a key and its value across two lines.
- Do not run it through a formatter or a "prettify JSON" tool.

If you edit by hand and keep the existing shape, you will be fine.

**3. Keep the placeholders.**
Some values contain `{0}` or `{1}`, for example `"group_created": "Group Created: {0}"`.
The mod substitutes real text there. Translate around them; do not remove,
renumber or translate them.

Save as UTF-8 without a BOM. Line endings do not matter - `.gitattributes`
normalizes them.

### Checking your file before opening a PR

Two things are worth confirming:

- The file still parses as JSON.
- Its key set matches `English.json`.

From the repository root:

```powershell
$ref = (Get-Content src\RecipePinner\Localization\English.json -Raw -Encoding UTF8 |
        ConvertFrom-Json).PSObject.Properties.Name
$mine = (Get-Content src\RecipePinner\Localization\Turkish.json -Raw -Encoding UTF8 |
         ConvertFrom-Json).PSObject.Properties.Name
Compare-Object $ref $mine
```

No output means the key sets match. `<=` marks a key you are missing, `=>` one
that should not be there.

If you can run the game, that is the better test: install the mod, set your
language, and check the My Pins panel and the Gathering List. With
`EnableDebugLogging = true` the BepInEx log reports how many strings were loaded.

## Reporting a bug

Please use the issue templates - they ask for the mod version, the BepInEx log
and your other mods, which is usually what decides whether a report can be acted
on. A bug report with the log attached is worth several without it.

## Code changes

Please **open an issue before writing code**.

Development happens in a separate private repository; this one is the copy
players see. That means a pull request against the source here cannot be merged
directly - the change has to be carried over by hand, and if it conflicts with
work already in progress it may not be usable at all. An issue first costs you
nothing and saves the effort of a patch that cannot land.

Bug reports, translations, documentation fixes and suggestions do not have this
problem and are always welcome.

## Building from source

See "Building From Source" in the [README](README.md). You need the .NET SDK and
a Valheim install with BepInEx, because the mod compiles against the game's own
assemblies. Those assemblies cannot be redistributed, which is also why this
repository has no build workflow.
