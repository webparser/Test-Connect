using System;
using System.Linq;
using System.Reflection;
using System.Windows;

using iFlow.Utils;
using iFlow.Wpf;

namespace iFlow.DataProvider
{
	/// <summary>
	/// Interaction logic for WndApplication.xaml
	/// </summary>
	public partial class WndApplication : Window
	{
		public WndApplication()
		{
			InitializeComponent();
			Title = AssemblyHelper.GetApplicationTitle();
			Left = -2000000;
			Instance = this;
		}

		private static WndApplication Instance;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			WndMain wndMain = RegisterWindow(new WndMain());
			wndMain.Show();
		}

		public static T RegisterWindow<T>(T window) where T : Window
		{
			window.Owner = Instance;
			window.Closed += Window_Closed;
			return window;
		}

		private static void Window_Closed(object sender, EventArgs e)
		{
			PropertyInfo propertyInfo = typeof(Application).GetProperty("IsShuttingDown", BindingFlags.Static | BindingFlags.NonPublic);
			bool isShuttingDown = (bool)propertyInfo.GetValue(null, null);
			if (!isShuttingDown)
				return;
			// Событие Unloaded не вызывается, если приложение завершается. Вызываем Unloaded вручную.
			foreach (FrameworkElement element in ((Window)sender).GetVisualTree<FrameworkElement>().ToArray())
			{
				const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
				MethodInfo[] methodInfos = element.GetType().GetMethods(flags).Where(x => x.Name.EndsWith("_Unloaded")).ToArray();
				foreach (MethodInfo methodInfo in methodInfos)
					if ((methodInfo.Attributes & MethodAttributes.SpecialName) == 0)
						ReflectionHelper.InvokeMethod(element, methodInfo.Name, element, null);
			}

		}

	}

}
