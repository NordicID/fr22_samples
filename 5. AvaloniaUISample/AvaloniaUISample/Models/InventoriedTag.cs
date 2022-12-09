using Avalonia.Threading;
using NurApiDotNet;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaUISample.Models
{
    public class InventoriedTagHistItem
    {
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
        public double Rssi { get; set; }
    }

    public class InventoriedTag : ReactiveObject
    {
        public List<InventoriedTagHistItem> History { get; set; }

        public InventoriedTag(NurApi.Tag tag)
        {
            History = new List<InventoriedTagHistItem>();
            EPC = tag.GetEpcString();
            RSSI = tag.rssi.ToString();

            History.Add(new InventoriedTagHistItem() { Rssi = tag.rssi });
        }

        public void Update(NurApi.Tag tag)
        {
            RSSI = tag.rssi.ToString();
            ReadCount++;

            History.Add(new InventoriedTagHistItem() { Rssi = tag.rssi });
            if (History.Count > 100) { 
                History.RemoveAt(0);
            }

            UiUpdated = true;
            TagUpdated?.Invoke(this, tag);
        }

        public event EventHandler<NurApi.Tag> ?TagUpdated;

        public bool UiAdded;
        public bool UiUpdated;

        public string EPC { get; internal set; }

        string mRssi = string.Empty;
        public string RSSI
        {
            get => mRssi;
            set => this.RaiseAndSetIfChanged(ref mRssi, value);
        }

        int mReadCount = 1;
        public int ReadCount
        {
            get => mReadCount;
            set => this.RaiseAndSetIfChanged(ref mReadCount, value);
        }
    }
}
