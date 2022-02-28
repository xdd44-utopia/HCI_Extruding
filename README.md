## 3D Modeling demo of a research in application of non-flat screen

By Unity (Version 2019.4.18f1)

Current setting is for two Samsung Note 20 Ultra with Android

## Documents:

[REVISION LOG](Revision%20log.md)

[TCP Protocol for information exchange between two phones](Protocol.md)

[Brief explanation of each class file](Class.md)

[Brief explanation of workflow of each function](Workflow.md)

[Brief explanation of construction of eye tracking 3D effect](3d.md)

## Installation

### Prerequisites

- Operating System: Mac OSX (Windows testing)
  
- Hardware: Two Android phones ([Support Google ARCore library](https://developers.google.com/ar/devices)) or iPhones (iOS 11.0 or above)

### Getting Started

- Install Unity 2019.4.18f1 with Android/iOS modules from [Unity download archive page](https://unity3d.com/get-unity/download/archive)
  
- (For Android) Install Gradle 5.6.4 [manually](https://gradle.org/install/) (since the latest Gradle version is v7.x) from [Gradle Releases page](https://gradle.org/releases/)
- (For iOS) Install [Xcode](https://apps.apple.com/us/app/xcode/id497799835?mt=12) and register an Apple Developer account
  
- [Clone this repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository)
  
  `git clone https://github.com/xdd44-utopia/HCI_Extruding.git`

### Unity Projects Setup

- This repository contains TWO Unity projects (two project root folders in repo root folder): *Server* for the application running on the LEFT phone, *Client* for RIGHT phone
  
- Add these projects to Unity Hub through *Open* function and open these two projects
- Set *Game* window to your phones' screen ratio/resolution in its top left corner's menu
  
  ![Setting Game window](/Files/Set%20game%20window.png)

- Test if the application works well on your computer by click *Run* button (At the top middle of Unity's window, in red circle below)

  ![Run the application on computer](/Files/Run%20the%20application%20on%20computer.png)

  If success, you may see differences like change of perspective. Normally, there should not be any errors appearing in console.

  ![Application running](/Files/Application%20running.png)

- (For Android) In Unity's preference (*Unity* Menu > Preferences... or Command + ,) > External Tools: Uncheck *Gradle Installed with Unity (recommended)*; Fill `/opt/gradle/gradle-5.6.4` to *Gradle* slot (Or your Gradle path if you didn't use the path in Gradle's manual)

### Phone Setup

- (For Android) Install *Google Play Services for AR* from both phones' *Google Play Store* or in this [link](https://play.google.com/store/apps/details?id=com.google.ar.core&hl=en&gl=US)
  
- Connect them to the same Wi-Fi (Or connect the left phone to the right phone's hotspot)
- (For iPhone) Take note of the left phone's IP address in Wi-Fi connection details, typically `xxx.xxx.xxx.xxx` (xxx denotes a number, often but not necessarily has three digits)

### Upload Projects to Phones

- Connect phones to computer
  
- (First time building) In Files > Build Settings...: Choose platform Android / iOS, click *Switch Platform*
  
- Choose Files > Build And Run or Command + B.
  
- (For Android) If success, the application will run on phones
  
- (For iOS) If success, an Xcode project will open automatically. A few things need to be checked before click *Run* button:
  1. In File > Project Settings..., make sure Build System is *New Build System (Default)*
  2. Make sure iPhone is connected and selected at the top of the window
  3. Select *Unity-iPhone project* at the left explorer, make sure settings are completed in *Signing & Capabilities*
  
  ![Xcode settings](/Files/Xcode%20setup.png)

  Click *Run* button at top left. If success, the application will be built to phones
  At the first time of building, the app may be untrusted by iOS. Go to Settings > General > VPN & Device Management to trust the profile, and then open the app.

  ![iOS setting 1](/Files/iOS%20setup%201.jpg)
  ![iOS setting 2](/Files/iOS%20setup%202.jpg)

- There's an input field on top of the RIGHT phone's screen. (For Android) Input the ip address displayed on bottom of the LEFT phone's screen / (For iPhone) Input the ip address recorded in Phone Setup Step. If success, the input field will disappear and the background of both phones will turn black.