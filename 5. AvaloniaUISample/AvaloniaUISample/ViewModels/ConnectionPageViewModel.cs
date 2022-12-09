using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using NurApiDotNet;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;

namespace AvaloniaUISample.ViewModels
{
    public class ConnectionPageViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> ConnectDeviceCmd { get; }

        public ObservableCollection<Uri> FoundDevices { get; }

        Uri? mSelectedDevice;
        public Uri? SelectedDevice
        {
            get => mSelectedDevice;
            set {
                ConnectionUri = (value==null) ? "" : value.ToString();
                this.RaiseAndSetIfChanged(ref mSelectedDevice, value);
            }
        }

        string mConnectionUri = "tcp://localhost/";
        public string ConnectionUri
        {
            get => mConnectionUri;
            set => this.RaiseAndSetIfChanged(ref mConnectionUri, value);
        }

        string mConnectButtonText = "Connect";
        public string ConnectButtonText
        {
            get => mConnectButtonText;
            set => this.RaiseAndSetIfChanged(ref mConnectButtonText, value);
        }

        string mConnectionStatusText = "Disconnected";
        public string ConnectionStatusText
        {
            get => mConnectionStatusText;
            set => this.RaiseAndSetIfChanged(ref mConnectionStatusText, value);
        }

        public ConnectionPageViewModel()
        {
            ConnectDeviceCmd = ReactiveCommand.Create(ConnectDevice);
            FoundDevices = new ObservableCollection<Uri>();

            App.NurApi.ConnectionStatusEvent += NurApi_ConnectionStatusEvent;

            FoundDevices.Add(new Uri("tcp://localhost"));
            if (!Design.IsDesignMode)
                NurDeviceDiscovery.Start(DeviceDiscoveryCallback);
        }

        private void NurApi_ConnectionStatusEvent(object ?sender, NurTransportStatus e)
        {
            NurApi ?api = (NurApi?)sender;
            if (api == null) return;

            if (e == NurTransportStatus.Connected)
            {
                try
                {
                    var info = api.GetReaderInfo();
                    var serial = info.altSerial.Length > 0 ? info.altSerial : info.serial;
                    ConnectionStatusText = $"Connected to {serial}";
                } 
                catch (Exception ex)
                {
                    ConnectionStatusText = $"Connected with error {ex.Message}";
                }
            } else
            {
                ConnectionStatusText = e.ToString();
            }

            if (e == NurTransportStatus.Disconnected)
            {
                ConnectButtonText = "Connect";
            } 
            else
            {
                ConnectButtonText = "Disconnect";
            }
        }

        void DeviceDiscoveryCallback(object sender, NurDeviceDiscoveryEventArgs args)
        {
            Dispatcher.UIThread.InvokeAsync(new Action(() => 
            {
                Uri? item = FoundDevices.FirstOrDefault(u => u.ToString()==args.Uri.ToString());

                if (args.Visible)
                {
                    Debug.WriteLine($"ADD '{args.Uri}'");
                    FoundDevices.Add(args.Uri);
                } 
                else if (item != null)
                {
                    Debug.WriteLine($"DEL '{args.Uri}'");
                    FoundDevices.Remove(item);
                }
            }));
        }

        async void ConnectDevice()
        {
            try
            {
                if (App.NurApi.ConnectionStatus == NurTransportStatus.Disconnected)
                {
                    Uri uri = new Uri(ConnectionUri);
                    App.NurApi.Connect(uri);
                }
                else
                {
                    App.NurApi.Disconnect();
                }
            } 
            catch (Exception ex)
            {
                await App.ShowException(ex);
            }
        }
    }
}
