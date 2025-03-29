using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MarketProject.ViewModels;
using MarketProject.Views;
using Microsoft.Extensions.DependencyInjection;
using MarketProject.Models;

namespace MarketProject;

public partial class App : Application
{

    private ServiceProvider _provider; // delcarando a váriavel de serviço
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var collection = new ServiceCollection();
        collection.AddSingleton<Database>(); // Declarado o serviço "Database"
        collection.AddSingleton<HomeViewModel>(); // Declardo o serviço "HomeViewModel"
      
        var services = collection.BuildServiceProvider(); // Implementando os serviços

        _provider = services;
        this.Resources[typeof(ServiceProvider)] = services; // Recebe os serviços estabelecidos na aplicação 
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LoginPage
            {
                DataContext = new LoginPageViewModel(),
            };
            desktop.Startup += (_, _) =>
            {
                _provider.GetRequiredService<Database>().StartStorage();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}