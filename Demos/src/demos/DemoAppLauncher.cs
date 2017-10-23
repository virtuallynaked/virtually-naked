using System;

class DemoAppLauncher {
	[STAThread]
	public static void Main(string[] args) {
		if (args.Length < 1) {
			Console.Error.WriteLine("missing demo-app type-name argument");
			return;
		}

		var typeName = args[0];

		var type = Type.GetType(typeName);
		if (type == null) {
			Console.Error.WriteLine("missing demo-app type {0}", typeName);
			return;
		}

		var obj = Activator.CreateInstance(type);

		if (obj is IDemoApp app) {
			app.Run();
		} else {
			Console.Error.WriteLine("demo app type {0} doesn't implement a recognized interface", typeName);
		}
	}
}

