using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace TVOutManager
{
	// The name TVOutOS4TestAppDelegate is referenced in the MainWindow.xib file.
	public partial class TVOutOS4TestAppDelegate : UIApplicationDelegate
	{
		private MainViewController vc;
		
		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// create a view controller dependent on the current device type
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
			{
				vc = new MainViewController("MainView-iPad");
			}
			else
			{
				vc = new MainViewController("MainView-iPhone");
			}
			
			// on screen ...
			window.AddSubview(vc.View);
			window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}

