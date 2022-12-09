using Avalonia.Controls;
using AvaloniaUISample.ViewModels;
using ReactiveUI;
using System.Diagnostics;
using System.Reactive;

namespace AvaloniaUISample.Views
{
    public partial class InventoryPage : UserControl
    {
        internal InventoryPageViewModel ?ViewModel => DataContext as InventoryPageViewModel;

        public InventoryPage()
        {
            DataContext = new InventoryPageViewModel();

            InitializeComponent();

            if (ViewModel != null)
                ViewModel.SelectedItemChanged += ViewModel_SelectedItemChanged;
        }

        private void ViewModel_SelectedItemChanged(object? sender, Models.InventoriedTag? e)
        {            
            this.FindControl<SingleTagInfoControl>("TagInfo")?.SelectTag(e);
        }
    }
}
