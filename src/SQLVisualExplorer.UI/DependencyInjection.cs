using Microsoft.Extensions.DependencyInjection;
using SQLVisualExplorer.UI.ViewModels;
using SQLVisualExplorer.UI.Views;

namespace SQLVisualExplorer.UI;

public static class DependencyInjection
{
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();

        return services;
    }
}
