namespace SampleAsyncTransactions
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Quik;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isDdeStarted;
		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			_securitiesWindow.MakeHideable();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			Path.Text = QuikTerminal.GetDefaultPath();
		}

		public QuikTrader Trader { get; private set; }

		public static MainWindow Instance { get; private set; }

		public Portfolio Portfolio
		{
			get { return Portfolios.SelectedPortfolio; }
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_securitiesWindow.DeleteHideable();
			_securitiesWindow.Close();

			if (Trader != null)
			{
				if (_isDdeStarted)
					StopDde();

				Trader.Dispose();
			}

			base.OnClosing(e);
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!Path.Text.IsEmpty())
				dlg.SelectedPath = Path.Text;

			if (dlg.ShowDialog(this) == true)
			{
				Path.Text = dlg.SelectedPath;
			}
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (Path.Text.IsEmpty())
				MessageBox.Show(this, LocalizedStrings.Str2969);
			else
			{
				Trader = new QuikTrader(Path.Text) { IsDde = true, IsAsyncMode = true };

				Portfolios.Connector = Trader;

				Trader.Connected += () => this.GuiAsync(() => ExportDde.IsEnabled = true);
				Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);

				// подписываемся на событие о неудачной регистрации заявок
				Trader.OrdersRegisterFailed += OrdersFailed;
				// подписываемся на событие о неудачном снятии заявок
				Trader.OrdersCancelFailed += OrdersFailed;

				// подписываемся на событие о неудачной регистрации стоп-заявок
				Trader.StopOrdersRegisterFailed += OrdersFailed;
				// подписываемся на событие о неудачном снятии стоп-заявок
				Trader.StopOrdersCancelFailed += OrdersFailed;

				// добавляем экспорт дополнительных колонок из стакана (своя покупка и продажа)
				Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnBidVolume);
				Trader.QuotesTable.Columns.Add(DdeQuoteColumns.OwnAskVolume);

				Trader.Connect();

				ShowSecurities.IsEnabled = true;
				ConnectBtn.IsEnabled = false;
			}
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
					MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str2982);
			});
		}

		private void StartDde()
		{
			Trader.StartExport();
			_isDdeStarted = true;
		}

		private void StopDde()
		{
			Trader.StopExport();
			_isDdeStarted = false;
		}

		private void ExportDdeClick(object sender, RoutedEventArgs e)
		{
			if (_isDdeStarted)
				StopDde();
			else
				StartDde();
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			if (_securitiesWindow.Visibility == Visibility.Visible)
				_securitiesWindow.Hide();
			else
				_securitiesWindow.Show();
		}
	}
}