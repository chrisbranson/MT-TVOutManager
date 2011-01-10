/*
 * Manager class for handling application display mirroring via the iPad and iPhone 4 video connector
 * 
 * Original objC class by Rob Terrell (rob@touchcentric.com) on 8/16/10
 * https://github.com/robterrell/TVOutManager
 * 
 * Original C# MonoTouch port by MonoTouch forum user Instbldrjems
 * http://forums.monotouch.net/yaf_postst1085.aspx
 * 
 * Additional work/tweaks/fixes by Chris Branson
 * https://github.com/chrisbranson/MT-TVOutManager
 * 
 * Note :- this video output code works only on iOS 4.0+
 * Video output is supported in the iPad, iPhone 4 and iPod Touch 4th Gen.
 * 
 * kUseBackgroundThread when set to true will use a thread to handle external screen updates,
 * when false it will use a system timer. I've had more reliable use using the thread.
 * 
 * kUseUIGetScreenImage when set to true will use a private API (UIGetScreenImage) to capture
 * screen content. The advantages to using this method is that the status bar is captured as are
 * some of the UI animations. When set to false an AppStore and simulator friendly method is used
 * which only captures the application window.
 * Note :- the simulator does NOT work with UIGetScreenImage, you'll only see a grey area.
 * 
 * There are a few minor bugs which I haven't tracked down yet:-
 * 
 * Sometimes when turning off TV Output the application will crash
 * Sometimes when disconnecting an external display while being used, the application will crash
 * If an external display is connected, the application run and then TV ouput enabled, the display will not always update correctly
 * The app is hyper sensitive to device rotation, minor movements to the device will have the external display spinning like a top
 * If the device is in landscape orientation when the app is first run, the TV output is wrongly orientated.
 * 
 * Most of these bugs are present in the original objC code, however in my use none of them have proven
 * to be a major pain, hence my lack of attention ...
 * 
 */

using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;
using MonoTouch.UIKit;
using System.Threading;

namespace TVOutManager
{
	public class TVOutManager
	{
		bool kUseBackgroundThread = true;
		bool kUseUIGetScreenImage = true;
		
		bool tvSafeMode = false;
		bool done = false; 
		double kFPS = 15;
		
		UIWindow tvoutWindow, deviceWindow;
		UIImageView mirrorView;
		UIImage image;
		NSTimer updateTimer;
		
		Thread thread;
		
		public TVOutManager ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver("UIScreenDidConnectNotification",
				(notify) => { startTVOut(); notify.Dispose(); });
			
			NSNotificationCenter.DefaultCenter.AddObserver("UIScreenDidDisconnectNotification",
				(notify) => { stopTVOut(); notify.Dispose(); });
			
			NSNotificationCenter.DefaultCenter.AddObserver("UIScreenModeDidChangeNotification",
				(notify) => { startTVOut(); notify.Dispose(); });
			
			UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
			NSNotificationCenter.DefaultCenter.AddObserver("UIDeviceOrientationDidChangeNotification",
				(notify) =>
				{
					if (mirrorView == null || done == true) { notify.Dispose(); return; }
				
					if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft)
					{
						UIView.BeginAnimations("turnLeft");
						mirrorView.Transform = CGAffineTransform.MakeRotation((float)(Math.PI * 1.5));
						UIView.CommitAnimations();
					}
					else if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight)
					{
						UIView.BeginAnimations("turnRight");
						mirrorView.Transform = CGAffineTransform.MakeRotation((float)(Math.PI * -1.5));
						UIView.CommitAnimations();
					}
					else 
					{
						UIView.BeginAnimations("turnUp");
						mirrorView.Transform = CGAffineTransform.MakeIdentity();
						UIView.CommitAnimations();
					}
				
					notify.Dispose();
				});
		}
			
		public void setTvSafeMode(bool val)
		{
			if (tvoutWindow != null)
			{
				if ((tvSafeMode == true) && (val == false))
				{
					UIView.BeginAnimations("zoomIn");
					tvoutWindow.Transform = CGAffineTransform.MakeScale(1.25f, 1.25f);
					UIView.CommitAnimations();
					tvoutWindow.SetNeedsDisplay();
				}
				else if ((tvSafeMode == false) && (val == true))
				{
					UIView.BeginAnimations("zoomOut");
					tvoutWindow.Transform = CGAffineTransform.MakeScale(0.8f, 0.8f);
					UIView.CommitAnimations();
					tvoutWindow.SetNeedsDisplay();
				}
			}
			
			tvSafeMode = val;
		}
		
		public void startTVOut()
		{
			if (UIApplication.SharedApplication.KeyWindow == null) return;
			
			if (UIScreen.Screens.Length <= 1)
			{
				UIAlertView al = new UIAlertView("TVOutManager", "startTVOut failed (no external screens detected", null, "Close", null); 
				al.Show();
				return;
			}
		
			if (tvoutWindow != null)
			{
				//tvOutWindow already exists, so this is a reconnected cable or a mode change
				tvoutWindow.Dispose();
				tvoutWindow = null;
			}
			
			if (tvoutWindow == null)
			{
				deviceWindow = UIApplication.SharedApplication.KeyWindow;
				
				SizeF max = new Size(0,0);
				UIScreenMode maxScreenMode = null;
				UIScreen external = UIScreen.Screens[1];
				
				for (int i = 0; i < external.AvailableModes.Length; i++)
				{
					UIScreenMode current = external.AvailableModes[i];
					if (current.Size.Width > max.Width)
					{
						max = current.Size;
						maxScreenMode = current;
					}
				}
				if (maxScreenMode != null) external.CurrentMode = maxScreenMode;
				
				tvoutWindow = new UIWindow(new RectangleF(0, 0, max.Width, max.Height));
				tvoutWindow.UserInteractionEnabled = false;
				tvoutWindow.Screen = external;
				
				//size the mirrorView to expand to fit the external screen
				RectangleF mirrorRect = UIScreen.MainScreen.Bounds;
				float horiz = max.Width / mirrorRect.Width;
				float vert = max.Height / mirrorRect.Height;
				float bigScale = horiz < vert ? horiz : vert;
				mirrorRect = new RectangleF(mirrorRect.X, mirrorRect.Y, mirrorRect.Width * bigScale, mirrorRect.Height * bigScale);
				
				mirrorView = new UIImageView(mirrorRect);
				mirrorView.Center = tvoutWindow.Center;
				
				//TV safe area -- scale the window by 20% -- for composite / component, not needed for VGA output
				if (tvSafeMode)
					tvoutWindow.Transform = CGAffineTransform.MakeScale(0.8f, 0.8f);
				else
					tvoutWindow.Transform = CGAffineTransform.MakeScale(1.25f, 1.25f);
					
				tvoutWindow.AddSubview(mirrorView);
				//mirrorView.Dispose();
				tvoutWindow.MakeKeyAndVisible();
				tvoutWindow.Hidden = false;
				tvoutWindow.BackgroundColor = UIColor.DarkGray;
				
				//orient the view properly
				if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft) {
					mirrorView.Transform = CGAffineTransform.MakeRotation((float)(Math.PI * 1.5));
				} else if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight) {
					mirrorView.Transform = CGAffineTransform.MakeRotation((float)(Math.PI * -1.5));
				}
				
				deviceWindow.MakeKeyAndVisible();
				
				updateTVOut();
				
				if (kUseBackgroundThread)
				{
					if (thread == null)
					{
						thread = new Thread(updateLoop as ThreadStart);
						thread.Start();
					}
				}
				else
				{
					updateTimer = NSTimer.CreateRepeatingScheduledTimer((1.0/kFPS), updateTVOut);
				}
			}
		}
		
		public void stopTVOut()
		{
			done = true;
			
			if (kUseBackgroundThread && thread != null)
			{
				thread.Abort();
				thread = null;
			}
			else if (updateTimer != null && updateTimer.IsValid)
			{
				updateTimer.Invalidate();
				updateTimer.Dispose();
				updateTimer = null;
			}
			
			if (tvoutWindow != null)
			{
				tvoutWindow.Dispose();
				tvoutWindow = null;
				mirrorView = null;
			}
		}
		
		[Export ("updateTVOut")]
		void updateTVOut()
		{
			if (kUseUIGetScreenImage)
			{
				// UIGetScreenImage() is no longer allowed in shipping apps, see https://devforums.apple.com/thread/61338
				// however, it's better for demos, since it includes the status bar and captures animated transitions

				using (CGImage cgScreen = CGImage.ScreenImage)
				{
					if (cgScreen != null)
					{
						if (mirrorView != null)
						{
							if (mirrorView.Image != null) mirrorView.Image.Dispose();
							mirrorView.Image = UIImage.FromImage(cgScreen);
						}
					}
				}
			}
			else
			{
				// from http://developer.apple.com/iphone/library/qa/qa2010/qa1703.html	
				// bonus, this works in the simulator; sadly, it doesn't capture the status bar
				//
				// if you are making an OpenGL app, use UIGetScreenImage() above or switch the
				// following code to match Apple's sample at http://developer.apple.com/iphone/library/qa/qa2010/qa1704.html
				// note that you'll need to pass in a reference to your eaglview to get that to work.

				UIGraphics.BeginImageContext(deviceWindow.Bounds.Size);
				var context = UIGraphics.GetCurrentContext();
				
				// get every window's contents (i.e. so you can see alerts, ads, etc.)
				foreach (UIWindow window in UIApplication.SharedApplication.Windows)
				{
					if (! (window.RespondsToSelector(new MonoTouch.ObjCRuntime.Selector("screen")))
					    || (window.Screen == UIScreen.MainScreen))
					{
						context.SaveState();
						context.TranslateCTM(window.Center.X, window.Center.Y);
						context.ConcatCTM(window.Transform);
						context.TranslateCTM(-(window.Bounds.Size.Width * window.Layer.AnchorPoint.X),
						                     -(window.Bounds.Size.Height * window.Layer.AnchorPoint.Y));
						window.Layer.RenderInContext(context);
						context.RestoreState();
					}
				}
				
				image = UIGraphics.GetImageFromCurrentImageContext();
				UIGraphics.EndImageContext();
				mirrorView.Image = image;
			}
		}
		
		void updateLoop()
		{
			using (var pool = new NSAutoreleasePool())
			{
				done = false;
				
				while (!done)
				{
					UIApplication.SharedApplication.InvokeOnMainThread(updateTVOut);
					Thread.Sleep((int)((1.0/kFPS)));
				}
			}
		}
	}
}