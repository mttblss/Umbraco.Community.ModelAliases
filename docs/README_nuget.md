# Model Aliases

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.ModelAliases?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.ModelAliases/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.ModelAliases?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.ModelAliases)
[![GitHub license](https://img.shields.io/github/license/mttblss/Umbraco.Community.ModelAliases?color=8AB803)](https://github.com/mttblss/Umbraco.Community.ModelAliases/blob/main/LICENSE)

Property alias constants for your Umbraco Models Builder models, so `ContentPage.Aliases.Title` is `"title"`
and you never type an alias string by hand again.

## What it does

Models Builder gives you a typed property for each field (`Model.Title`), but a lot of Umbraco code still asks for
the alias as a string: `Value<T>("title")`, `HasValue("title")`. Those strings are easy to mistype and nothing tells
you when they go stale. This package adds a nested `Aliases` class to every generated model, with one `const string`
per property:

```csharp
// Before: a typo compiles, and quietly returns 0
var width = media.Value<int>("umbracoWidht");

// After: a typo is a build error
var width = media.Value<int>(Image.Aliases.UmbracoWidth);
```

The constants are written whenever Models Builder generates your models, into a separate `<Model>.Aliases.gen.cs`
file beside each stock `<Model>.generated.cs`. Your existing models are not edited.

## Benefits

- **No magic strings.** A mistyped alias fails the build instead of returning an empty value at runtime.
- **IntelliSense.** Type `ContentPage.Aliases.` and pick from the list.
- **Stays in step with your content types.** Rename or remove a property and every place that used its constant
  stops compiling.
- **Everything the model exposes**, including properties from compositions and base types.
- **Real constants**, so they work in `switch` case labels and attribute arguments.
- **No setup.** Nothing to configure and nothing in your existing models to change.

## Installation

Add the package to the Umbraco website that runs Models Builder (v17.0.0 or later):

`dotnet add package Umbraco.Community.ModelAliases`

## Usage

1. Set `Umbraco:CMS:ModelsBuilder:ModelsMode` to `SourceCodeManual` or `SourceCodeAuto`.
2. In the back office go to Settings, Models Builder, and press **Generate models**.
3. Rebuild, then use the constants. They sit in your models' namespace:

```razor
@if (Model.HasValue(ContentPage.Aliases.Banner))
{
    <p>@Model.Value<string>(ContentPage.Aliases.Title)</p>
}
```

The constant is named after the property, so alias `pageTitle` becomes `Aliases.PageTitle`.

## Good to know

- Commit the `*.Aliases.gen.cs` files with your other generated models. A site that only consumes pre-built models
  (`ModelsMode: Nothing`, as on Umbraco Cloud) gets them that way.
- Do not rename the files to end `.generated.cs`: Umbraco deletes any of those it did not write itself.
- A property named `Aliases`, or two properties that end up with the same name, stops generation with a clear
  message. Change one of the aliases.
- Requires Umbraco 17.0.0 or later, on .NET 10.
