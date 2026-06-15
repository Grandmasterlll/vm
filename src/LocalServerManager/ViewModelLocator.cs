using LocalServerManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LocalServerManager;

public class ViewModelLocator
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public SettingsViewModel SettingsViewModel =>
        _serviceProvider.GetRequiredService<SettingsViewModel>();
}
