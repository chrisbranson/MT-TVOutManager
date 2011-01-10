/*
 * This view controller shows an example page for use with the TV Out Manager
 * 
 * Original objC code by Rob Terrell (rob@touchcentric.com) on 8/16/10
 * https://github.com/robterrell/TVOutManager
 * 
 * Additional work/tweaks/fixes by Chris Branson
 * https://github.com/chrisbranson/MT-TVOutManager
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace TVOutManager
{
	public partial class MainViewController : UIViewController
	{
		NSTimer myTicker;
		private TVOutManager mgr;
		
		public MainViewController (string nibName) : base(nibName, null)
		{
			// add observers for each of the screen notifications
			NSNotificationCenter.DefaultCenter.AddObserver("UIScreenDidConnectNotification",
		    	(notify) => { screenDidConnectNotification(); notify.Dispose();	});
			
			NSNotificationCenter.DefaultCenter.AddObserver("UIScreenDidDisconnectNotification",
		    	(notify) => { screenDidDisconnectNotification(); notify.Dispose(); });
			
			NSNotificationCenter.DefaultCenter.AddObserver("UIScreenModeDidChangeNotification",
		    	(notify) => { screenModeDidChangeNotification(); notify.Dispose(); });
		}
		
		public override void ViewDidLoad ()
		{
			// start the time for our clock
			runTimer();
			base.ViewDidLoad ();
			
			// init the TV Out Manager
			mgr = new TVOutManager();
		}
		
		void runTimer()
		{
			// This starts the timer which fires the showActivity method every 0.5 seconds
			myTicker = NSTimer.CreateRepeatingScheduledTimer(0.5f, showActivity);
		}
		
		void showActivity()
		{
			// update the clock label text with the current date/time
			clockLabel.Text = DateTime.Now.ToString();
		}
		
		partial void toggleVideoOutput (UISwitch sender)
		{	
			if (videoSwitch.On)
				mgr.startTVOut();
			else
				mgr.stopTVOut();
		}
		
		partial void toggleSafeMode (UISwitch sender)
		{
			mgr.setTvSafeMode(tvSwitch.On);
		}
		
		partial void showInfo (UIButton sender)
		{
			// show the 'about' page
			FlipsideViewController vc = new FlipsideViewController(this);
			vc.ModalTransitionStyle = UIModalTransitionStyle.FlipHorizontal;
			this.PresentModalViewController(vc, true);
		}
		
		internal void flipsideViewControllerDidFinish(FlipsideViewController vc)
		{
			this.DismissModalViewControllerAnimated(true);
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}
		
		public void screenDidConnectNotification()
		{
			infoLabel.Text = string.Format("Screen connected");
		}
		
		public void screenDidDisconnectNotification()
		{
			infoLabel.Text = string.Format("Screen disconnected");
		}
		
		public void screenModeDidChangeNotification()
		{
			infoLabel.Text = string.Format("Screen mode changed");
		}
	}
}