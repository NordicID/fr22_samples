using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using AvaloniaUISample.ViewModels;
using AvaloniaUISample.Views;
using NurApiDotNet;
using System.Threading.Tasks;
using System;

namespace AvaloniaUISample
{
    public partial class App : Application
    {
        static public NurApi NurApi = new NurApi();

        static public async Task ShowException(Exception ex)
        {
            string txt = string.Format("Exception\n{0}", ex.Message);
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandardWindow("Error", ex.Message);
            await messageBoxStandardWindow.Show();
        }

        public override void Initialize()
        {
            // NurApi.SetLogLevel(NurApi.LOG_ERROR | NurApi.LOG_VERBOSE);
            NurApi.LogEvent += NurApi_LogEvent;
            AvaloniaXamlLoader.Load(this);
        }

        private void NurApi_LogEvent(object? sender, NurApi.LogEventArgs e)
        {
            Console.WriteLine(e.message);
            System.Diagnostics.Debug.WriteLine(e.message);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// override RegisterServices register custom service
        /// </summary>
        public override void RegisterServices()
        {
            base.RegisterServices();
        }
    }
}
