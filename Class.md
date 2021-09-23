# Brief description about all classes (.cs script files)

## Server:

[Button Controller](Server/Assets/Scripts/ButtonController.cs)  
A stupid way for the arrow buttons to change the eye position in manual mode.

[Camera Controller](Server/Assets/Scripts/CameraController.cs)  
Control the position, field of view and clipping depth of the camera to create 3D effect.

[Face Tracker](Server/Assets/Scripts/FaceTracker.cs)  
Ultilize the AR face tracking function in AR extension and transform it into eye position in application coordinate.

[Grid Controller](Server/Assets/Scripts/GridController.cs)  
Control the scale of the ruler grid.

[Mesh Manipulater](Server/Assets/Scripts/MeshManipulator.cs)  
Include most of the transforming and manipulating functions working on the object.

[Object Controller](Server/Assets/Scripts/ObjectController.cs)  
Include functions for visualizing, highlighting and simplifying the object.

[Server Controller](Server/Assets/Scripts/ServerController.cs)  
See [Document of TCP Protocol](Protocol.md).

[Slice Controller](Server/Assets/Scripts/SliceController.cs)  
Control the position and angle of the cutting plane.

[Slice Trace Visualizer](Server/Assets/Scripts/SliceTraceVisualizer.cs)  
Draw the visualization line for cross screen cutting.

[Slider Controller](Server/Assets/Scripts/SliderController.cs)  
Calculate the angle between two phones

[Touch Processer](Server/Assets/Scripts/TouchProcessor.cs)  
Handle all touch events detected from both phones

## Client

[Camera Controller](Server/Assets/Scripts/CameraController.cs)  
Control the position, field of view and clipping depth of the camera to create 3D effect.

[Client Controller](Server/Assets/Scripts/ClientController.cs)  
See [Document of TCP Protocol](Protocol.md).

[Cutting Plane Controller](Server/Assets/Scripts/CuttingPlaneController.cs)  
Control the screen as cutting plane.

[Extrude Handle](Server/Assets/Scripts/ExtrudeHandle.cs)  
Control the visibility of the extrude handle

[Face Tracker](Server/Assets/Scripts/FaceTracker.cs)  
Ultilize the AR face tracking function in AR extension and transform it into eye position in application coordinate, and send it to server phone

[Grid Controller](Server/Assets/Scripts/GridController.cs)  
Synchronize the scale of the ruler grid to server phone.

[Light Controller](Server/Assets/Scripts/LightController.cs)  
Synchronize the lighting angle to server phone.

[Object Controller](Server/Assets/Scripts/ObjectController.cs)  
Receive and visualize mesh information from server phone

[Slice Trace Visualizer](Server/Assets/Scripts/SliceTraceVisualizer.cs)  
Draw the visualization line for cross screen cutting.

[Slider Controller](Server/Assets/Scripts/SliderController.cs)  
Control the slider which indicates the angle between two phones

[Touch Processer](Server/Assets/Scripts/TouchProcessor.cs)  
Detect touch events and send them to server phone