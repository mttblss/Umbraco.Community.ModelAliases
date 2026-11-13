# Umbraco.Community.ModelAliases

Gives every Models Builder model a nested `Aliases` class of `const string` property aliases:
`ContentPage.Aliases.Title == "title"`. It is a separate file per model, extending the stock partial class, written
by decorating Umbraco's `IModelsGenerator`.

This file is the reasoning behind the code. Read it before changing anything here.

---

## The output file name must not end `.generated.cs`

Our files are `<Model>.Aliases.gen.cs`. Stock `ModelsGenerator.GenerateModels()` ends by deleting every
`*.generated.cs` in the models folder that it did not write in that run. A name ending `.generated.cs` would be
deleted by Umbraco on every generation. Nothing warns you; the files just keep vanishing. The suffix is
`ModelAliasesWriter.FileSuffix`, and the same constant drives our own stale-file cleanup.

## The seam is `IModelsGenerator`, because there is no notification

Models Builder raises no event before or after generation (checked in the 17.7.2 source). The dashboard
button, the Management API `build` endpoint and `SourceCodeAuto` all call `IModelsGenerator.GenerateModels()`,
and Umbraco registers it with a plain `AddSingleton`, so a later registration wins. That is all this project
relies on.

- **It must register after Umbraco's.** `AddWebsite()` calls `AddModelsBuilder()`; composers run after it
  (`AddComposers()`). Register earlier and Umbraco's registration silently replaces ours.
- **The inner generator is built with `ActivatorUtilities`, not resolved.** Umbraco also registers the
  concrete `ModelsGenerator`, but its own comment marks that for removal.
- **A decorator, not a subclass.** Subclassing `ModelsGenerator` means `new` on a non-virtual method and a
  constructor copied from Umbraco's, which breaks when that signature changes.

## Order inside `GenerateModels()` is load-bearing

1. Build every file in memory. A naming clash throws here, with the models folder untouched.
2. Write ours (skipping identical content) and delete alias files for types that no longer exist.
3. Run Umbraco's generator **last**. It deletes `ood.flag` on success, so the flag only clears if every step
   succeeded. Run it first and a failure in ours would leave models marked current while the aliases are stale.

Writing only when content differs keeps timestamps and `git status` quiet, as stock does. The header carries
no Umbraco version, deliberately: stock puts it in every file, so each Umbraco upgrade rewrites them all.

## What an `Aliases` class contains

Everything the model exposes: its own properties, its compositions' (recursively) and its base types'.
`TypeModel.Properties` holds only a type's own properties, and a nested class is not inherited usefully, so a
class built from it alone would be nearly empty (a page type's `Title` and `Content` typically come from
compositions).

- Constant names are `PropertyModel.ClrName`, so `ContentPage.Title` and `ContentPage.Aliases.Title` always agree.
- Properties Umbraco could not generate (`Errors` set) are skipped; there is no instance property to match.
- A model with no properties still gets an empty class, so `Type.Aliases` always exists.
- **It throws** if a property is called `Aliases` (C# forbids a member named like its enclosing class) or two
  aliases land on one name. Generation fails for those two cases even though stock generation would not have.
- The walk reimplements what Umbraco's internal `TypeModel.CollectImplems` does. If an Umbraco upgrade changes how
  compositions are generated, the two can disagree; the check below catches it.

**After an Umbraco upgrade, compare the sets:** for every model, the `[ImplementPropertyType("…")]` aliases in
`X.generated.cs` must equal the constants in `X.Aliases.gen.cs`.

## The nested class is `Aliases`, not `Properties`

Every model inherits a public instance `Properties` (`PublishedContentWrapped`, `IPublishedElement`). A nested
type with that name would hide it, so `contentPage.Properties` would stop compiling on concrete model types. That
rests on the C# member-lookup rules and was **not** compiled; `Properties` was rejected on that reasoning. What
was compiled: with `Aliases`, `ContentPage.Properties` still resolves, and the constants work in constant
expressions and `switch` case labels.

`new` is always emitted (under a `0109` pragma, like the stock files) so a derived model does not warn about
hiding a base model's `Aliases`.

## Constants, not static properties

`const string` works in attributes and `switch` labels; a static property does not. It still reads
`Aliases.Title`.

## The package has no reference to the consumer's models

It reads the namespace and folder from the Models Builder settings and takes its data from `UmbracoServices`,
so it references only `Umbraco.Cms.Core` and `Umbraco.Cms.Infrastructure`. A reference to a models project would
be a cycle, because that project is where the files are written.

- **The Umbraco floor is 17.0.0, with 17.7.2 the newest version tested.** It leans on Models Builder types
  (`TypeModel`, `UmbracoServices`, `IModelsGenerator`) that Umbraco does not promise to keep, so "any 17.x" rests
  on a diff of the 17.0.0 and 17.7.2 source for everything this package touches: the generator and its callers and
  registration, `UmbracoServices`, `TypeModel`, `PropertyModel`, `ModelsBuilderSettings`, `ModelsDirectoryAbsolute`
  and `Constants.ModelsBuilder`. Only comments and formatting differ there; the one code change in the folder is
  `TextBuilder` (generic type arguments), which this package does not call. Re-run that diff, and the checks below,
  before raising the floor or claiming a later major.
- **The dependency range is capped at `[17.0.0, 18.0.0)`** (`src/Directory.Packages.props`). The Marketplace reads
  supported Umbraco versions from the package's NuGet dependency and treats an open-ended one as working on every
  later major, which would claim Umbraco 18 untested. Capping means an Umbraco 18 site cannot install this until a
  release raises the cap, after the diff and checks above have been repeated for 18. Rejected: leaving it open
  and setting `VersionDependencyMode` to `SemVer`, which tells the Marketplace to believe the open range.
- **The NU1902/NU1903 warnings** (`System.Security.Cryptography.Pkcs`, and `MailKit`/`MimeKit` at 17.0.0) come
  from Umbraco's own dependencies and clear when Umbraco moves. They are not pinned here. A site that installs the
  package resolves its own, newer, Umbraco.
- **There is no client project.** The repo was set up from Lotte Pitcher's Opinionated Package Starter, but its
  `umbraco-extension` step (Vite, TypeScript, an example dashboard) was left out, as this package has no back-office
  UI. The package project is a plain `Microsoft.NET.Sdk` library, not Razor.

## Rejected alternatives

- **Regex-editing the stock `.generated.cs` in place.** What Søren Kottal's article (dev.to, "Customizing
  Umbraco ModelsBuilder Output") does, with a nested `ModelPropertyAliases`. It depends on the stock output's exact
  text (an insertion marker that, if absent, silently does nothing) and edits Umbraco's file. Ours takes its data
  from `TypeModel` and never touches a stock file, and a separate file was the requirement.
- **`Limbo.Umbraco.ModelsBuilder`.** Replaces the generator (`ModelsMode` must be `Nothing`), v17 was alpha only
  when this was checked, and it documents no extra files or alias constants. Not read beyond its README.
- **Aliases on the interfaces (`IPageTitle`).** The class already carries the full set; nothing needs them there.

## Decided limitations

- **Only where models are generated.** That means `SourceCodeManual` or `SourceCodeAuto`. A site that only
  consumes pre-built models (`ModelsMode: Nothing`, as on Umbraco Cloud) gets the files by committing them from a
  development machine.
- **The Build endpoint returns 200 even when generation throws.** The error goes to `models.err` and the dashboard,
  so a caller of `POST …/models-builder/build` cannot tell it failed. Check `models.err`.
- **`GetAllTypes()` runs twice** (ours and the stock generator's). A few cached service calls on a manual button
  press, so not optimised.
- **A rebuild and restart is still needed** to use new models, as with all generated models.

## What is proven, and where

The code was written and tested inside a site (mttblss, Umbraco 17.7.2), then moved here. In that site: live
generation wrote all 24 files and left stock files untouched; every model's constants matched its stock
`[ImplementPropertyType]` aliases; a second run changed nothing (timestamps held); a stale alias file was deleted.
After the move, the writer's edge cases (clash, a property called `Aliases`, composition cycles, de-duplication,
skipped errors, escaping, sorting) were re-run against this package's assembly and passed.

The `TestSite` here is on `ModelsMode: SourceCodeManual`, writing to `umbraco/models` (git-ignored: it is output,
and the site has no content types of its own beyond Umbraco's built-in media and member types). Generating there
wrote an alias file for each of its 8 models beside the stock file, and the models compiled. That ran on both
17.7.2 and 17.0.0 (a fresh database each time), and on 17.0.0 every model's constants equal its stock
`[ImplementPropertyType]` aliases.

**Not exercised:**
- The package installed from NuGet (every run so far is a project reference).
- `ood.flag` appearing after a real content-type change and clearing afterwards.
- A real clash reaching `models.err` through the endpoint (it would mean changing live document types).
- The 17.x versions between 17.0.0 and 17.7.2. They are covered by the source diff, not by a run.
- Umbraco 18 and later.
