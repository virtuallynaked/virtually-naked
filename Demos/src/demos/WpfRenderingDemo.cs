using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

public class WpfRenderingDemo : IDemoApp {
	public void Run() {
		RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

		var visualizer = new MenuViewMessageInterpreter();
		var state = new MenuViewMessage {
			Items = {
				MenuItemViewMessage.MakeRange("[-1, 1]: 0.5", -1, +1, 0.5, false),
				MenuItemViewMessage.MakeRange("[-1, 1]: -1", -1, +1, -1, true),
				MenuItemViewMessage.MakeRange("[-1, 1]: 0", -1, +1, 0, false),
				MenuItemViewMessage.MakeRange("[-1, 1]: +1", -1, +1, +1, false),
				MenuItemViewMessage.MakeRange("[0, 1]: 0.5", 0, 1, 0.5, false),
				MenuItemViewMessage.MakeRange("[0, 1]: 0", 0, 1, 0, false),
				MenuItemViewMessage.MakeRange("[0, 1]: 1", 0, 1, 1, false)
			}
		};
		var visual = visualizer.Interpret(state);

		var window = new Window {
			Background = Brushes.Pink,
			Content = visual
		};
		window.Show();
		
		Dispatcher.Run();
	}
}
