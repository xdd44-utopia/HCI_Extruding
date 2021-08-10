## 3D Modeling demo of a research in application of non-flat screen

By Unity

### TCP Message

## From server to client:

Mesh:
- First line "Mesh"
- Third line $nv$ for $nv$ vertices
- Followed by one line of vertices separated by comma
- Fourth line $nt$ for $nt$ triangle indices
- Followed by one line of indices separated by comma

Transform:
- First line "Transform"
- Second Line $n$ for $n$th objects
- Third line position
- Fourth line rotation quaternion
- Fifth line scale

Highlight:
- First line "Highlight"
- Second line -1 or highlight face index

Face Track:
- First line "Face"
- Second Line Vector3 or '0' for ortho

Slice Visualize:
- First line "Slice"
- Fourth Line n
- Next n Vector3

Cutting Plane Visualize:
- First line "Cutting"
- Second Line Vector3 touchPointThisScreen
- Third Line Vector3 touchPointOtherScreen
- Fourth Line Vector3 touchStartThisScreen
- Fifth Line Vector3 touchStartOtherScreen

Angle
- First line "Angle"
- Second line angle in radius

Grid scale
- First line "Grid"
- Second line float point number

Extrude handle
- First line "Extrude"
- Second line extrude distance

## From client to server:

"Hello": confirm connection

Touching:
- First line "Touch"
- Second line n
- Next n line Vector3 position + previous position
- Next n line touch phase (Began, Moved, Stationary, Ended, Canceled)

Acceleration:
- First line "Acc"
- Second line acceleration

Cutting:
- First line "Cutting"
- Second line touch point in server's space

Face tracking:
- First line "Face"
- Second line face position or 'X'



todo
1. cut a diamond (video)
2. Change edge snapping to edge align when moving towards the edge
3. Change rotating gesture to two-finger together with scaling
4. Add highlight to snapped face and indication of fit angle between two faces
5. Change snapping gesture to button
6. Show a cutting "blade" when the edge is aligned and the angle fits
7. Remove constrains of edge snapping