iPhone App Video Mirroring for iOS 4
====================================

Manager class and example code for handling application display mirroring via the iPad and iPhone 4 video connector

Credits
-------
 
Original objC class by Rob Terrell (rob@touchcentric.com) on 8/16/10
https://github.com/robterrell/TVOutManager

Original C# MonoTouch port by MonoTouch forum user Instbldrjems
http://forums.monotouch.net/yaf_postst1085.aspx

Additional work/tweaks/fixes by Chris Branson
https://github.com/chrisbranson/MT-TVOutManager

Compatibility Note
------------------

Note :- this video output code works only on iOS 4.0+
Video output is supported in the iPad, iPhone 4 and iPod Touch 4th Gen.

TVOutManager Class Reference
----------------------------

kUseBackgroundThread when set to true will use a thread to handle external screen updates,
when false it will use a system timer. I've had more reliable use using the thread.

kUseUIGetScreenImage when set to true will use a private API (UIGetScreenImage) to capture
screen content. The advantages to using this method is that the status bar is captured as are
some of the UI animations. When set to false an AppStore and simulator friendly method is used
which only captures the application window.
Note :- the simulator does NOT work with UIGetScreenImage, you'll only see a grey area.

-startTVOut
Creates a window on the second screen at the highest resolution it supports, and starts a timer (at the frames per second rate defined in the class file) to copy the screen contents to the window. If no screen is attached, -startTVOut will simply report a failure to the console.

-stopTVOut
Stops the periodic video-mirror timer (or thread) and releases the offscreen window.

setTvSafeMode (bool)
When tvSafeMode is YES, the class will scale down the output size by 20%, so that the entire picture can fit within the visible scan area of an analog TV. If you don't know what an analog TV is, don't worry, you'll probably never see one.

Bugs
----

There are a few minor bugs which I haven't tracked down yet:-

Sometimes when turning off TV Output the application will crash
Sometimes when disconnecting an external display while being used, the application will crash
If an external display is connected, the application run and then TV ouput enabled, the display will not always update correctly
The app is hyper sensitive to device rotation, minor movements to the device will have the external display spinning like a top
If the device is in landscape orientation when the app is first run, the TV output is wrongly orientated.
 
Most of these bugs are present in the original objC code, however in my use none of them have proven to be a major pain, hence my lack of attention ...