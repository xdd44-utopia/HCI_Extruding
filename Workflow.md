# Brief description about implementation of functions

## Touch event

1. Touch events are detected at `TouchProcessor.Update()`.
2. (For client phone) Touch events are sent to server phone.
3. Touch events are processed into `enum Status` at `TouchProcessor.Calculate()`.

## Select

1. Extract touchPosition from touch events and set it to `MeshManipulator.touchPosition`.
2. Call `MeshManipulator.castRay()`, which performs ray casting and sends index of hit triangle to `ObjectController.selectFace()`.
3. `ObjectController.selectFace()` checks the selection states of the triangle in question, and returns the result indicating selecting or canceling.
4. In main loop `ObjectController.updateHighlight()` will update colors of surfaces being highlighted.

## Snap

1. The snapping button will call `startNewFocusThisScreen()` or `startNewFocusOtherScreen()` which will call `startFocus()`. (This extra step exists because there're parameters for script-called `startFocus()`)
2. `startFocus()` calculates the angle and position the oject needs to transform.
3. `focus()` runs in main loop to perform the snapping animation.

## Transform (moving, rotating, scaling)

1. Extract `panThisScreen`, `panOtherScreen`, `turnThisScreen`, `turnOtherScreen`, `pinchDelta` from touch events.
2. The values above are sent to `MeshManipulator.startMoving()`, `MeshManipulator.startRotating()`, `MeshManipulator.startScaling()`.
3. `MeshManipulator.startScaling()` may call `startFocus()` to keep the object snapped.
4. `MeshManipulator.startMoving` may call `adjustAlign()` to align the object to the edge of the screen.

## Mesh change

To understand everything here, knowledge about the concept "polygon mesh" is needed.  
Suggest readings: [Wikipedia "Polygon Mesh"](https://en.wikipedia.org/wiki/Polygon_mesh), [Unity Manual "Meshes"](https://docs.unity3d.com/Manual/comp-MeshGroup.html), [Unity Script Reference "Mesh"](https://docs.unity3d.com/ScriptReference/Mesh.html)

1. `ObjectController.isMeshUpdated` is used for others to notify `ObjectController` of change in mesh
2. If changed. `simplifyMesh()` will be executed first.
3. The first two steps in `simplifyMesh()` is `updateFaces()`, which groups coplanar triangles in meshes to actual "faces", and `updateEdges()`, which lists the vertices of outlines of every faces.
4. "Simplify edges" section in `simplifyMesh()` turns every outline into simpliest, if there're colinear vertices in an edge.
5. "Reconstruct faces" section works when there're unnecessary number of triangles in one face (If a face is `n`-sided polygon, then `n-2` triangles are needed). [Ear clipping algorithm](https://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf) is used here to construct `n-2` triangles from outline with length `n`.
6. Lastly, useless vertices will be removed.
7. `updateVisualization()` includes `updateFaces()`, `updateEdges()` and an extra `updateCovers()`. Since Unity doesn't have edge display, surfaces slightly smaller than each faces are used here to fake "edges".
8. `sendMesh()` will compose the TCP message to send the mesh to client phone.

## Cross Screen Gesture Cut

1. `TouchProcessor.Calculate()` calls `TouchProcessor.processCrossScreenSlice()` to reposition the cutting plane and `TouchProcessor.visualizeCrossScreenSlice()` to visualize.
2. `TouchProcessor.processCrossScreenSlice()` calls `MeshManipulator.startSlice()` -> `MeshManipulator.prepareSlice()` to do all the calculation according to current cutting plane's position and angle and store the result (but not actually cut yet).
3. When `TouchProcessor` thinks the user releases his hand (`crossScreenSliceTimer <= 0 && slicePrepared` in `TouchProcessor.update()`), it will call `MeshManipulator.executeSlice()`, which will apply the result to mesh.
4. Then `ObjectController` will be notified there's change in mesh.

## Screen Cut (Cut Button)

Similar and simplier than Cross Screen Gesture Cut. It just gets executed by `MeshManipulator.enableCuttingPlaneOtherScreen()` and the other 3 funtions from the buttons instead of from `TouchProcessor`

## Taper

1. The button calls `MeshManipulator.prepareTaper()` to calculate essential values like the scale center, but doesn't execute taper.
2. `TouchProcessor` calls `MeshManipulator.updateTaperScale()` to update the target scale of the selected face.
3. `MeshManipulator.taper()` is executed from main loop to apply the updated taper scale to the mesh.

## Extrude

Note: Extruding is allowed only when `MeshManipulator.isEdgeAligned == true`, see `MeshManipulator.adjustAlign()`  
1. `TouchProcessor` calls `MeshManipulator.updateExtrudeScale()` to update the length of the extrusion.
2. `MeshManipulator.prepareExtrude()` will be executed at first to prepare essential works such as separating the selected face and constructing new faces at the side.
3. `MeshManipulator.extrude()` is executed from main loop to appy the updated extrusion length to the mesh.