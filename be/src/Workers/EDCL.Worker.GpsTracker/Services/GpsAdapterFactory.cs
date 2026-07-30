using EDCL.Module.Driver.Domain.Enums;
using EDCL.Worker.GpsTracker.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace EDCL.Worker.GpsTracker.Services;

public interface IGpsAdapterFactory
{
    IGpsVendorAdapter? CreateAdapter(GpsProviderType providerType);
}

public sealed class GpsAdapterFactory(IServiceProvider serviceProvider) : IGpsAdapterFactory
{
    public IGpsVendorAdapter? CreateAdapter(GpsProviderType providerType)
    {
        return providerType switch
        {
            GpsProviderType.Innovatrack => serviceProvider.GetRequiredService<InnovatrackAdapter>(),
            GpsProviderType.Jitra => serviceProvider.GetRequiredService<JitraAdapter>(),
            GpsProviderType.Muliatrack => serviceProvider.GetRequiredService<MuliatrackAdapter>(),
            GpsProviderType.Puninar => serviceProvider.GetRequiredService<PuninarAdapter>(),
            _ => null
        };
    }
}
