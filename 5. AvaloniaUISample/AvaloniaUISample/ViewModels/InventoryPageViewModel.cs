using Avalonia.Threading;
using AvaloniaUISample.Models;
using AvaloniaUISample.Utils;
using NurApiDotNet;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaUISample.ViewModels
{
    public class InventoryPageViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> InventoryCmd { get; }
        public ReactiveCommand<Unit, Unit> InventoryStreamCmd { get; }

        public ObservableCollection<InventoriedTag> InventoriedTags { get; }
        Dictionary<string, InventoriedTag> InventoriedTagsDict = new Dictionary<string, InventoriedTag>();

        string mInvButtonText = "Simple Inventory";
        public string InvButtonText 
        {
            get => mInvButtonText;
            set => this.RaiseAndSetIfChanged(ref mInvButtonText, value);
        }

        string mInvStreamButtonText = "Start Inventory Stream";
        public string InvStreamButtonText
        {
            get => mInvStreamButtonText;
            set => this.RaiseAndSetIfChanged(ref mInvStreamButtonText, value);
        }

        bool mInvButtonState = false;
        public bool InvButtonState
        {
            get => mInvButtonState;
            set => this.RaiseAndSetIfChanged(ref mInvButtonState, value);
        }

        string mInventoryInfo = "Idle";
        public string InventoryInfo
        {
            get => mInventoryInfo;
            set => this.RaiseAndSetIfChanged(ref mInventoryInfo, value);
        }

        object ?mSelectedItem = null;
        public object ?SelectedItem
        {
            get => mSelectedItem;
            set
            {
                this.RaiseAndSetIfChanged(ref mSelectedItem, value);
                SelectedItemChanged?.Invoke(this, value as InventoriedTag);
            }
        }

        public event EventHandler<InventoriedTag?> ?SelectedItemChanged;

        bool mStreamRunning = false;
        DispatcherTimer mUiUpdateTimer;

        // Stats
        static double TAGS_PER_SEC_OVERTIME = 2;
        private AvgBuffer mTagsPerSecBuffer = new AvgBuffer(1000, (int)(TAGS_PER_SEC_OVERTIME * 1000));

        private long mTagsReadTotal = 0;
        private double mTagsPerSec = 0;
        private double mAvgTagsPerSec = 0;
        private double mMaxTagsPerSec = 0;
        private int mInventoryRounds = 0;
        private double mTagsFoundInTime = 0;
        Stopwatch mInventoryStart = new Stopwatch();

        public InventoryPageViewModel()
        {
            InventoryCmd = ReactiveCommand.Create(Inventory);
            InventoryStreamCmd = ReactiveCommand.Create(InventoryStream);

            InventoriedTags = new ObservableCollection<InventoriedTag>();

            App.NurApi.InventoryStreamEvent += NurApi_InventoryStreamEvent;
            App.NurApi.ConnectionStatusEvent += NurApi_ConnectionStatusEvent;

            // Setup UI update timer, every 250ms
            mUiUpdateTimer = new DispatcherTimer();
            mUiUpdateTimer.Tick += MUiUpdateTimer_Tick;
            mUiUpdateTimer.Interval = TimeSpan.FromMilliseconds(250);
        }

        private void NurApi_ConnectionStatusEvent(object? sender, NurTransportStatus e)
        {
            // Update UI
            mStreamRunning = false;
            InvButtonState = (e == NurTransportStatus.Connected);
            InvButtonText = "Simple Inventory";
            InvStreamButtonText = "Start Inventory Stream";
        }

        private void MUiUpdateTimer_Tick(object ?sender, object ?e)
        {
            lock (InventoriedTagsDict)
            {
                // Update UI listview from unique tag dictionary
                foreach (var pair in InventoriedTagsDict)
                {
                    if (!pair.Value.UiAdded)
                    {
                        InventoriedTags.Add(pair.Value);
                        pair.Value.UiAdded = true;
                        pair.Value.UiUpdated = false;
                    }
                    else if (pair.Value.UiUpdated)
                    {
                        pair.Value.RaisePropertyChanged();
                        pair.Value.UiUpdated = false;
                    }
                }

                // Update stats to UI
                InventoryInfo = string.Format("Tags: Total {0} Unique {1} In time {5:0.0}  |  Speed: Now {2:0.0} Avg {3:0.0} Peak {4:0.0}  |  Inventory Rounds {6}",
                    mTagsReadTotal, InventoriedTagsDict.Count,
                    mTagsPerSec, mAvgTagsPerSec, mMaxTagsPerSec,
                    mTagsFoundInTime,
                    mInventoryRounds);
            }
        }        

        void ClearStats()
        {
            mTagsReadTotal = 0;
            mTagsPerSec = 0;
            mAvgTagsPerSec = 0;
            mMaxTagsPerSec = 0;
            mInventoryRounds = 0;
            mTagsFoundInTime = 0;
            mInventoryStart.Stop();
            mInventoryStart.Reset();
        }

        void UpdateStats(InventoryStreamData ev)
        {
            mTagsPerSecBuffer.Add(ev.tagsAdded);
            mTagsReadTotal += ev.tagsAdded;

            mTagsPerSec = mTagsPerSecBuffer.SumValue / TAGS_PER_SEC_OVERTIME;
            if (mInventoryStart.ElapsedMilliseconds > 1000)
                mAvgTagsPerSec = mTagsReadTotal / ((double)mInventoryStart.ElapsedMilliseconds / 1000.0);
            else
                mAvgTagsPerSec = mTagsPerSec;

            if (mTagsPerSec > mMaxTagsPerSec)
                mMaxTagsPerSec = mTagsPerSec;

            mInventoryRounds += ev.roundsDone;
        }

        private void NurApi_InventoryStreamEvent(object ?sender, NurApi.InventoryStreamEventArgs e)
        {
            // Update stats
            UpdateStats(e.data);

            // Update unique tags
            if (UpdateInventoriedTags())
            {
                // New tag(s) found. set time
                mTagsFoundInTime = ((double)mInventoryStart.ElapsedMilliseconds / 1000.0);
            }

            // Restart stream if needed
            if (mStreamRunning && e.data.stopped)
            {
                try
                {
                    App.NurApi.StartInventoryStream();
                }
                catch (Exception ex) {
                    // This should not ever happen
                    Console.WriteLine(ex);
                }
            }
        }

        async void Inventory()
        {
            try
            {
                // Set UI state while inventory running
                InvButtonState = false;
                InvButtonText = "In progress..";
                InventoriedTags.Clear();
                lock (InventoriedTagsDict)
                    InventoriedTagsDict.Clear();
                ClearStats();

                // Execute inventory in own task
                await Task.Run(() =>
                {
                    // Clear tag storage
                    App.NurApi.ClearTagsEx();

                    // Perform simple inventory
                    mInventoryStart.Start();
                    NurApi.InventoryResponse resp = App.NurApi.Inventory(0, 0, 0);
                    mInventoryStart.Stop();

                    // Update stats
                    mTagsFoundInTime = ((double)mInventoryStart.ElapsedMilliseconds / 1000.0);
                    mTagsReadTotal = resp.numTagsFound;
                    mInventoryRounds = resp.roundsDone;

                    // Read tags from device
                    App.NurApi.FetchTags();
                });

                // Update our tag storage
                UpdateInventoriedTags();

                // Update UI
                MUiUpdateTimer_Tick(mUiUpdateTimer, null);
            }
            catch (Exception ex)
            {
                await App.ShowException(ex);
            }
            finally
            {
                // Update UI
                InvButtonState = App.NurApi.IsConnected();
                InvButtonText = "Simple Inventory";
            }
        }

        async void InventoryStream()
        {
            try
            {
                InvButtonState = false;
                if (!mStreamRunning)
                {
                    // Set UI state while starting
                    InvStreamButtonText = "Starting..";
                    InventoriedTags.Clear();
                    lock (InventoriedTagsDict)
                        InventoriedTagsDict.Clear();
                    ClearStats();

                    // Start inventory stream in own task
                    await Task.Run(() =>
                    {
                        // Enable zero readings for stats update
                        App.NurApi.EnableInvStreamZeros = true;
                        // Clear tag storage
                        App.NurApi.ClearTagsEx();
                        // Start stream
                        mInventoryStart.Start();
                        App.NurApi.StartInventoryStream();
                        mStreamRunning = true;                        
                    });

                    mUiUpdateTimer.Start();
                }
                else
                {
                    mUiUpdateTimer.Stop();

                    // Set UI while stopping
                    InvStreamButtonText = "Stopping..";

                    // Stop stream in own task
                    await Task.Run(() =>
                    {
                        mStreamRunning = false;
                        // Stop
                        App.NurApi.StopInventoryStream();
                        // Reset zero readings
                        App.NurApi.EnableInvStreamZeros = false;
                    });
                }
            }
            catch (Exception ex)
            {
                await App.ShowException(ex);
            }
            finally
            {
                // Update UI
                InvButtonState = App.NurApi.IsConnected();
                if (mStreamRunning)
                    InvStreamButtonText = "Stop Inventory Stream";
                else
                    InvStreamButtonText = "Start Inventory Stream";
            }
        }

        bool UpdateInventoriedTags()
        {
            bool ret = false;
            NurApi.TagStorage ts = App.NurApi.GetTagStorage();
            // Access to NurApi tag storage must be synchronized
            lock (ts)
            {
                lock (InventoriedTagsDict)
                {
                    foreach (NurApi.Tag tag in ts)
                    {
                        if (InventoriedTagsDict.ContainsKey(tag.GetEpcString()))
                        {
                            // Tag updated
                            InventoriedTag iTag = InventoriedTagsDict[tag.GetEpcString()];
                            iTag.Update(tag);
                        }
                        else
                        {
                            // New tag added
                            ret = true;
                            InventoriedTag iTag = new InventoriedTag(tag);
                            InventoriedTagsDict.Add(tag.GetEpcString(), iTag);
                        }
                    }
                }
                ts.Clear();
            }
            return ret;
        }
    }
}
