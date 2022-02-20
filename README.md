## 3D Modeling demo of a research in application of non-flat screen

By Unity (Version 2019.4.18f1)

Current setting is for two Samsung Note 20 Ultra with Android

## Documents:

[REVISION LOG](3d.md)

[TCP Protocol for information exchange between two phones](Protocol.md)

[Brief explanation of each class file](Class.md)

[Brief explanation of workflow of each function](Workflow.md)

[Brief explanation of construction of eye tracking 3D effect](3d.md)

## Installation

### Prerequisites

- Operating System: Mac OSX (Windows testing)
  
- Hardware: Two Android phones ([Support Google ARCore library](https://developers.google.com/ar/devices)) (iOS testing)

### Getting Started

- Install Unity 2019.4.18f1 with Android modules from [Unity download archive page](https://unity3d.com/get-unity/download/archive)
  
- Install Gradle 5.6.4 [manually](https://gradle.org/install/) (since the latest Gradle version is v7.x) from [Gradle Releases page](https://gradle.org/releases/)
  
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

- In Unity's preference (*Unity* Menu > Preferences... or Command + ,) > External Tools: Uncheck *Gradle Installed with Unity (recommended)*; Fill `/opt/gradle/gradle-5.6.4` to *Gradle* slot (Or your Gradle path if you didn't use the path in Gradle's manual)

### Phone Setup

- Install *Google Play Services for AR* from both phones' *Google Play Store* or in this [link](https://play.google.com/store/apps/details?id=com.google.ar.core&hl=en&gl=US)
  
- Connect them to the same Wi-Fi (Or connect the left phone to the right phone's hotspot)
- Find the left phone's IP address in its settings, typically `xxx.xxx.xxx.xxx` (xxx denotes a number, often but not necessarily has three digits)
- Go to ClientController.cs and fill the IP adress in the last public method `public void connect()`

### Upload Projects to Phones

- Connect phones to computer
  
- (First time building) In Files > Build Settings...: Choose platform Android, click *Switch Platform*
  
- Choose Files > Build And Run or Command + B. If success, the application will run on phones

- Tap *Connect* to connect. If success, backgrounds of the applications will turn black.