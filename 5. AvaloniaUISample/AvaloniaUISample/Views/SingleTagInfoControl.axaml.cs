using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaUISample.Models;
using AvaloniaUISample.ViewModels;
using Microcharts.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AvaloniaUISample.Views
{
    public partial class SingleTagInfoControl : UserControl
    {
        internal SingleTagInfoControlViewModel? ViewModel => DataContext as SingleTagInfoControlViewModel;

        DispatcherTimer mChartUpdateTimer;
        
        bool mChartNeedsUpdate = false;
        ChartView mChartView;

        public SingleTagInfoControl()
        {
            DataContext = new SingleTagInfoControlViewModel();

            InitializeComponent();

            mChartView = this.FindControl<Microcharts.Avalonia.ChartView>("RssiChart");

            // Setup chart update timer, every 250ms
            mChartUpdateTimer = new DispatcherTimer();
            mChartUpdateTimer.Tick += MChartUpdateTimer_Tick;
            mChartUpdateTimer.Interval = TimeSpan.FromMilliseconds(250);
        }

        private void MChartUpdateTimer_Tick(object? sender, EventArgs? e)
        {
            if (mChartNeedsUpdate)
            {
                mChartView.InvalidateVisual();
                mChartNeedsUpdate = false;
            }
        }

        public void SelectTag(InventoriedTag ?tag)
        {
            // Debug.WriteLine($"SelectTag {tag?.EPC}");

            mChartUpdateTimer.Stop();
            mChartNeedsUpdate = true;

            if (ViewModel?.SelectedTag != null)
                ViewModel.SelectedTag.TagUpdated -= SelectedTag_TagUpdated;

            if (ViewModel != null)
                ViewModel.SelectedTag = tag;

            if (ViewModel?.SelectedTag != null)
            {
                ViewModel.SelectedTag.TagUpdated += SelectedTag_TagUpdated;
                mChartUpdateTimer.Start();
            }
        }

        private void SelectedTag_TagUpdated(object? sender, NurApiDotNet.NurApi.Tag e)
        {
            mChartNeedsUpdate = true;            
        }
    }
}
