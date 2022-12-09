using Avalonia.Threading;
using AvaloniaUISample.Models;
using Microcharts;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AvaloniaUISample.ViewModels
{
    public class SingleTagInfoControlViewModel : ViewModelBase
    {
        List<ChartEntry> mRssiChartEntries = new List<ChartEntry>();

        Chart mRssiChart = new LineChart()
        {
            LabelTextSize = 10,
            LabelOrientation = Orientation.Horizontal,
            ValueLabelOrientation = Orientation.Horizontal,
            LineSize = 1,
            LineAreaAlpha = 0,
            PointSize = 1,
            PointMode = PointMode.None,
            MinValue = -90,
            MaxValue = -25,
            LineMode = LineMode.Straight,
            BackgroundColor = SKColors.LightBlue,
            Margin = 1,
        };

        public SingleTagInfoControlViewModel()
        {
            mRssiChart.Entries = mRssiChartEntries;
        }

        InventoriedTag? mSelectedTag;
        public InventoriedTag ?SelectedTag
        {
            get => mSelectedTag; 
            set
            {
                if (mSelectedTag != null)
                    mSelectedTag.TagUpdated -= MSelectedTag_TagUpdated;

                mSelectedTag = value;
                EPC = mSelectedTag?.EPC ?? "-";

                lock (mRssiChartEntries)
                {
                    mRssiChartEntries.Clear();
                    
                    if (mSelectedTag != null)
                    {
                        foreach (var item in mSelectedTag.History)
                        {
                            mRssiChartEntries.Add(new ChartEntry((float)item.Rssi));
                        }

                        mSelectedTag.TagUpdated += MSelectedTag_TagUpdated;
                    }
                }
            }
        }

        private void MSelectedTag_TagUpdated(object? sender, NurApiDotNet.NurApi.Tag tag)
        {
            lock (mRssiChartEntries)
            {
                mRssiChartEntries.Add(new ChartEntry(tag.rssi));
                if (mRssiChartEntries.Count > 100)
                    mRssiChartEntries.RemoveAt(0);
            }
        }

        string mEpc = "-";
        public string EPC
        {
            get => mEpc;
            set => this.RaiseAndSetIfChanged(ref mEpc, value);
        }

        public Chart RssiChart {
            get => mRssiChart;
            set => this.RaiseAndSetIfChanged(ref mRssiChart, value);
        }
    }
}
