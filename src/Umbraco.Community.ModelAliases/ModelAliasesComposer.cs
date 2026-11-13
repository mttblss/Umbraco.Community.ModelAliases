using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;

namespace Umbraco.Community.ModelAliases;

/// <summary>
/// Replaces <see cref="IModelsGenerator"/> with <see cref="AliasModelsGenerator"/>. Runs automatically,
/// after Umbraco's own <c>AddModelsBuilder()</c> (called from <c>AddWebsite()</c>), so this registration wins.
/// </summary>
public sealed class ModelAliasesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IModelsGenerator>(services => new AliasModelsGenerator(
            // Not services.GetRequiredService<ModelsGenerator>(): Umbraco registers that concrete type only
            // for old callers and marks it for removal.
            ActivatorUtilities.CreateInstance<ModelsGenerator>(services),
            services.GetRequiredService<UmbracoServices>(),
            services.GetRequiredService<IOptionsMonitor<ModelsBuilderSettings>>(),
            services.GetRequiredService<IHostEnvironment>()));
    }
}
