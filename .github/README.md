# Model Aliases

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.ModelAliases?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.ModelAliases/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ModelAliases?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ModelAliases)
[![GitHub license](https://img.shields.io/github/license/mttblss/Umbraco.Community.ModelAliases?color=8AB803)](../LICENSE)

Property alias constants for your Umbraco Models Builder models, so `ContentPage.Aliases.Title` is `"title"`
and you never type an alias string by hand again.

## What it does

Models Builder gives you a typed property for each field on a content type (`Model.Title`), but a lot of Umbraco
code still asks for the alias as a string: `Value<T>("title")`, `HasValue("title")`, `GetProperty("title")`.
Those strings are easy to mistype, and nothing tells you when they go stale.

This package adds a nested `Aliases` class to every generated model, holding one `const string` per property:

```csharp
// Before: a typo compiles, and quietly returns 0
var width = media.Value<int>("umbracoWidht");

// After: a typo is a build error
var width = media.Value<int>(Image.Aliases.UmbracoWidth);
```

The constants are written whenever Models Builder generates your models, into a separate file beside each stock
one (`Image.generated.cs` gets `Image.Aliases.gen.cs`). Your existing models are not edited.

## Benefits

- **No magic strings.** A mistyped alias fails the build instead of returning an empty value at runtime.
- **IntelliSense.** Type `ContentPage.Aliases.` and pick from the list.
- **Stays in step with your content types.** The constants regenerate with your models. Rename or remove a
  property and every place that used its constant stops compiling, so you find it before your visitors do.
- **Everything the model exposes.** Each class includes the properties a model gets from its compositions and
  its base types, not only the ones it declares itself. `ContentPage.Aliases` lists the same properties as the
  `ContentPage` class.
- **Real constants.** `const` values work in `switch` case labels and attribute arguments, where a static
  property does not.
- **No setup.** There is nothing to configure, and nothing in your existing models to change.
- **Does not get in the way.** The class is called `Aliases`, not `Properties`, so it does not hide the
  `Properties` member every Umbraco model already has.

## Installation

Add the package to the Umbraco website that runs Models Builder (any v17 release, from 17.0.0):

```bash
dotnet add package Umbraco.Community.ModelAliases
```

## Usage

1. **Use a generating Models Builder mode.** Set `Umbraco:CMS:ModelsBuilder:ModelsMode` to `SourceCodeManual` or
   `SourceCodeAuto`. The files are written when models are generated, which `InMemoryAuto` does not do to disk.
2. **Generate your models.** In the back office go to Settings, Models Builder, and press **Generate models**
   (or let `SourceCodeAuto` do it). Each model now has an `<Model>.Aliases.gen.cs` file beside its
   `<Model>.generated.cs` file.
3. **Rebuild**, then use the constants. They sit in your models' namespace.

```razor
@using Umbraco.Cms.Web.Common.PublishedModels @* or your own ModelsNamespace *@

@if (Model.HasValue(ContentPage.Aliases.Banner))
{
    <p>@Model.Value<string>(ContentPage.Aliases.Title)</p>
}
```

```csharp
// Anywhere a property alias is expected, including constant expressions
switch (propertyAlias)
{
    case ContentPage.Aliases.Title:
        // ...
        break;
}
```

The constant is named after the property on the model, so alias `pageTitle` becomes `Aliases.PageTitle`, the same
name as the `PageTitle` property it belongs to.

### What gets generated

```csharp
namespace Umbraco.Cms.Web.Common.PublishedModels
{
	public partial class Image
	{
		public new static class Aliases
		{
			public const string UmbracoBytes = "umbracoBytes";
			public const string UmbracoExtension = "umbracoExtension";
			public const string UmbracoFile = "umbracoFile";
			public const string UmbracoHeight = "umbracoHeight";
			public const string UmbracoWidth = "umbracoWidth";
		}
	}
}
```

(Shortened: the real file also carries a header and a `#pragma` around the class.)

## Good to know

- **Commit the `*.Aliases.gen.cs` files** with your other generated models. If your live site only consumes
  pre-built models (`ModelsMode: Nothing`, as on Umbraco Cloud), those committed files are how it gets them. The
  package only does anything where models are generated.
- **Rebuild after generating**, as with any generated model.
- **Do not rename the files to end `.generated.cs`.** Umbraco deletes every `*.generated.cs` it did not write
  itself, so those files would vanish on each generation. That is why these end `.gen.cs`.
- **Two cases stop generation with a clear message:** a property whose name is `Aliases` (C# does not allow a
  member named like its enclosing class), and two properties from different compositions that end up with the same
  name. Change one of the aliases. Umbraco reports the error the way it does for its own, in `models.err` and on
  the Models Builder dashboard.
- **Requirements:** Umbraco 17 (any release from 17.0.0), on .NET 10. It is not yet available for Umbraco 18.

## Contributing

Contributions to this package are most welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md).

## Acknowledgments

- Søren Kottal's article, "Customizing Umbraco ModelsBuilder Output" on dev.to, describes the same idea of nested
  alias constants. This package is written independently and reads Umbraco's type models rather than editing the
  generated text.
- Set up with Lotte Pitcher's [Opinionated Package Starter](https://github.com/LottePitcher/opinionated-package-starter).
