namespace SampleRss
{
	using System;
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.Rss;

	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;
		private RssTrader _trader;

		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			if (_trader != null)
				_trader.Dispose();

			base.OnClosed(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (_isConnected)
			{
				_trader.UnRegisterNews();
				_trader.StopExport();

				_trader.Dispose();
				_isConnected = false;
				return;
			}

			var address = AddressComboBox.SelectedAddress;

			if (address == null)
			{
				MessageBox.Show(this, LocalizedStrings.Str3804);
				return;
			}

			_trader = new RssTrader { Address = address };

			_trader.Disconnected += () => ChangeConnectStatus(false);
			_trader.ExportStopped += () => _trader = null;

			_trader.Connected += () =>
			{
				ChangeConnectStatus(true);

				_trader.StartExport();
			};

			// подписываемся на событие запуска экспорта, и запускаем подписку на новости
			_trader.ExportStarted += _trader.RegisterNews;

			_trader.NewNews += news => NewsPanel.NewsGrid.News.Add(news);

			_trader.ProcessDataError += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			_trader.Connect();
			_isConnected = true;
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			this.GuiAsync(() => ConnectBtn.Content = isConnected ? LocalizedStrings.Str2961 : LocalizedStrings.Str2962);
		}
	}
}