using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace TVOutManager
{
	public partial class FlipsideViewController : UIViewController
	{
		private MainViewController _parent;
		
		public FlipsideViewController (MainViewController parent)
		{
			_parent = parent;
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			//this.view.BackgroundColor = viewFlipsideBackgroundColor
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}
		
		partial void done (UIBarButtonItem sender)
		{
			_parent.flipsideViewControllerDidFinish(this);
		}
	}
	
}

