using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using InventoryApp.Data;
using InventoryApp.Services;
using InventoryApp.ViewModels;
using InventoryApp.Views;
using Microsoft.Extensions.Configuration;

namespace InventoryApp;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Make bindings format numbers/currency using the user's regional settings.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Composition root.
            var factory = new DbConnectionFactory(configuration);
            await factory.EnsureDatabaseAsync();

            IPartRepository parts = new PartRepository(factory);
            ICategoryRepository categories = new CategoryRepository(factory);
            IDialogService dialogs = new DialogService(parts, categories);

            var window = new MainWindow(new MainViewModel(parts, categories, dialogs));
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The application could not start.\n\n{ex.Message}\n\nCheck the connection string in appsettings.json and make sure MySQL is running.",
                "Inventory Management System", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred.\n\n{e.Exception.Message}",
            "Inventory Management System", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
