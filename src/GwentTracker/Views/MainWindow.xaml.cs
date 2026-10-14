using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using GwentTracker.ViewModels;
using ReactiveUI;
using Serilog;

namespace GwentTracker.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private DataGrid Cards => this.FindControl<DataGrid>("Cards");
        private DataGrid Opponents => this.FindControl<DataGrid>("Opponents");
        private TabControl MainTabs => this.FindControl<TabControl>("MainTabs");
        private ItemsControl Messages => this.FindControl<ItemsControl>("Messages");
        private TextBox FilterString => this.FindControl<TextBox>("FilterString");
        private TextBox OpponentFilterString => this.FindControl<TextBox>("OpponentFilterString");
        private ComboBox OpponentStatusFilter => this.FindControl<ComboBox>("OpponentStatusFilter");
        private ItemsControl Filters => this.FindControl<ItemsControl>("Filters");
        private ProgressBar LoadGameProgress => this.FindControl<ProgressBar>("LoadGameProgress");
        private Grid SelectedCard => this.FindControl<Grid>("SelectedCard");
        private Button AddFilter => this.FindControl<Button>("AddFilter");
        private Button ClearOpponentOverrides => this.FindControl<Button>("ClearOpponentOverrides");
        private Button OpenOpponentSource => this.FindControl<Button>("OpenOpponentSource");
        private TextBlock Status => this.FindControl<TextBlock>("Status");
        private TextBlock OpponentSummary => this.FindControl<TextBlock>("OpponentSummary");
        
        public MainWindow()
        {
            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Cards, v => v.Cards.Items).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Opponents, v => v.Opponents.Items).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.OpponentStatusFilters, v => v.OpponentStatusFilter.Items).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Messages, v => v.Messages.Items).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.FilterString, v => v.FilterString.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.OpponentFilterString, v => v.OpponentFilterString.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.OpponentStatusFilter, v => v.OpponentStatusFilter.SelectedItem).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Filters, v => v.Filters.Items).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.OpponentSummary, v => v.OpponentSummary.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.LoaderVisibility, v => v.LoadGameProgress.IsVisible).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CardVisibility, v => v.SelectedCard.IsVisible).DisposeWith(d);
                this.WhenAnyValue(v => v.Cards.SelectedItem).BindTo(this, w => w.ViewModel.SelectedCard).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.SelectedOpponent, v => v.Opponents.SelectedItem).DisposeWith(d);
                this.WhenAnyValue(v => v.MainTabs.SelectedIndex).BindTo(this, w => w.ViewModel.SelectedTabIndex).DisposeWith(d);
                this.WhenAnyValue(v => v.ViewModel.LoadCards).SelectMany(x => x.Execute()).Subscribe().DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.AddFilter, v => v.AddFilter).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ClearOpponentOverrides, v => v.ClearOpponentOverrides).DisposeWith(d);
                Observable
                    .FromEventPattern<EventHandler<RoutedEventArgs>, RoutedEventArgs>(
                        handler => OpenOpponentSource.Click += handler,
                        handler => OpenOpponentSource.Click -= handler)
                    .Subscribe(_ => OpenSelectedOpponentSource())
                    .DisposeWith(d);
                ViewModel.Notifications.Subscribe(Notify).DisposeWith(d);
                Observable
                    .FromEventPattern<EventHandler<KeyEventArgs>, KeyEventArgs>(
                        handler => FilterString.KeyDown += handler,
                        handler => FilterString.KeyDown -= handler)
                    .Where(e => e.EventArgs.Key == Key.Enter)
                    .Select(e => Unit.Default)
                    .InvokeCommand(this, v => v.ViewModel.AddFilter)
                    .DisposeWith(d);
                // Remove Filter binding is done inside xaml since button is part of data template
            });
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private IDisposable _timer;

        private void OpenSelectedOpponentSource()
        {
            var sourcePage = ViewModel?.SelectedOpponent?.SourcePage;
            if (!Uri.TryCreate(sourcePage, UriKind.Absolute, out var sourceUri) ||
                (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(sourceUri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Unable to open opponent source page {sourcePage}", sourceUri);
                Notify("Unable to open source page");
            }
        }

        private void Notify(string message)
        {
            _timer?.Dispose();
            Status.Text = message;
            _timer = Observable.Timer(ViewModel.NotificationDuration, AvaloniaScheduler.Instance)
                        .Subscribe(x => Status.Text = "");
        }
    }
}
