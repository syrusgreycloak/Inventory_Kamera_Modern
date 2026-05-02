using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using InventoryKamera.Avalonia.ViewModels;

namespace InventoryKamera.Avalonia.Views
{
    public partial class ScanView : UserControl
    {
        private INotifyCollectionChanged _consoleSubscribed;

        public ScanView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        // Auto-scroll the console ScrollViewer to the bottom whenever a new entry is
        // appended. Without this the user can't see live errors during a scan — by the
        // time they scroll manually the scan has moved on.
        private void OnDataContextChanged(object sender, EventArgs e)
        {
            // Detach prior subscription if the DataContext was swapped (e.g. unit tests).
            if (_consoleSubscribed != null)
            {
                _consoleSubscribed.CollectionChanged -= OnConsoleChanged;
                _consoleSubscribed = null;
            }

            if (DataContext is ScanViewModel vm && vm.Console is INotifyCollectionChanged ncc)
            {
                _consoleSubscribed = ncc;
                _consoleSubscribed.CollectionChanged += OnConsoleChanged;
            }
        }

        private void OnConsoleChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            // Marshal to UI thread; the source ObservableCollection mutates from worker
            // threads via the NLog target, but UI access must be on the dispatcher.
            Dispatcher.UIThread.Post(() => ConsoleScrollViewer?.ScrollToEnd(),
                                     DispatcherPriority.Background);
        }
    }
}
