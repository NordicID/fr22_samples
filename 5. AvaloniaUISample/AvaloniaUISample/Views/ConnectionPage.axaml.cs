using Avalonia.Controls;
using AvaloniaUISample.ViewModels;

namespace AvaloniaUISample.Views
{
    public partial class ConnectionPage : UserControl
    {
        internal ConnectionPageViewModel? ViewModel => DataContext as ConnectionPageViewModel;

        public ConnectionPage()
        {
            DataContext = new ConnectionPageViewModel();

            InitializeComponent();
        }
    }
}
