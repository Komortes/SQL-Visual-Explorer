using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SQLVisualExplorer.Application;
using SQLVisualExplorer.Infrastructure;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.UI;
using SQLVisualExplorer.UI.Views;

namespace SQLVisualExplorer.Desktop;

public sealed partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = ConfigureServices();
        InitializeDatabase(_services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services
            .AddApplicationServices()
            .AddInfrastructureServices()
            .AddUiServices();

        return services.BuildServiceProvider();
    }

    private static void InitializeDatabase(ServiceProvider services)
    {
        using var scope = services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ILocalDatabaseInitializer>();

        initializer.InitializeAsync().GetAwaiter().GetResult();
    }
}
