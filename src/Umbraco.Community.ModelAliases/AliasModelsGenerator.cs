using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;
using Umbraco.Extensions;

namespace Umbraco.Community.ModelAliases;

/// <summary>
/// Wraps Umbraco's <see cref="IModelsGenerator"/> and writes the alias files beside the models it generates.
/// </summary>
internal sealed class AliasModelsGenerator : IModelsGenerator
{
    private readonly IModelsGenerator _inner;
    private readonly UmbracoServices _umbracoServices;
    private readonly IOptionsMonitor<ModelsBuilderSettings> _config;
    private readonly IHostEnvironment _hostEnvironment;

    public AliasModelsGenerator(
        IModelsGenerator inner,
        UmbracoServices umbracoServices,
        IOptionsMonitor<ModelsBuilderSettings> config,
        IHostEnvironment hostEnvironment)
    {
        _inner = inner;
        _umbracoServices = umbracoServices;
        _config = config;
        _hostEnvironment = hostEnvironment;
    }

    public void GenerateModels()
    {
        ModelsBuilderSettings settings = _config.CurrentValue;
        var modelsNamespace = string.IsNullOrWhiteSpace(settings.ModelsNamespace)
            ? Constants.ModelsBuilder.DefaultModelsNamespace
            : settings.ModelsNamespace;
        var directory = settings.ModelsDirectoryAbsolute(_hostEnvironment);

        // Built before anything is written, so a name clash throws with the models directory untouched.
        IReadOnlyDictionary<string, string> files = ModelAliasesWriter.Write(_umbracoServices.GetAllTypes(), modelsNamespace);

        Directory.CreateDirectory(directory);

        foreach ((var fileName, var content) in files)
        {
            var path = Path.Combine(directory, fileName);

            // Left alone when identical, as the stock generator does, so timestamps and git stay quiet.
            if (File.Exists(path) && File.ReadAllText(path) == content)
            {
                continue;
            }

            File.WriteAllText(path, content);
        }

        foreach (var path in Directory.GetFiles(directory, "*" + ModelAliasesWriter.FileSuffix))
        {
            if (!files.ContainsKey(Path.GetFileName(path)))
            {
                File.Delete(path);
            }
        }

        // Last on purpose: it deletes ood.flag, which must only clear once everything above has succeeded.
        _inner.GenerateModels();
    }
}
