using System.Globalization;
using System.Threading;
using System.Windows;
using iFlow.Utils;

namespace iFlow.DataProvider
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class WndMain : Window
	{
		public WndMain()
		{
			InitializeComponent();
			Title = AssemblyHelper.GetMainWindowTitle();
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
			DataContext = new VmMain(Dispatcher);
		}

		private void Window_Closed(object sender, System.EventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}
