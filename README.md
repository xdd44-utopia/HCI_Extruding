## 3D Modeling demo of a research in application of non-flat screen

By Unity

### TCP Message

## From server to client:

Mesh:
- First line "Mesh"
- Second Line $n$ for $n$th mesh
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
- Second line "Object" or "Face"
- Third line $n$ for nth object OR $nv$ for $nv$ vertices
- Followed by one line of vertices separated by comma
- Fourth line $nt$ for $nt$ triangle indices
- Followed by one line of indices separated by comma

Face Track:
- First line "Face"
- Second Line Vector3 or '0' for ortho

Slice Visualize 
- First line "Slice"
- Second Line Vector3 touchPointThisScreen
- Third Line Vector3 touchPointOtherScreen
- Fourth Line n
- Next n Vector3

Angle
- First line "Angle"
- Second line angle in radius

## From client to server:

"Hello": confirm connection

Touching:
- First line "Touch"
- Second line n
- Next n line Vector3 position + previous position

Acceleration:
- First line "Acc"
- Second line acceleration

Cutting:
- First line "Cutting"
- Second line touch point in server's space

Extruding:
- First line "Extruding"
- Second line extruding distance

Face tracking:
- First line "Face"
- Second line face position or 'X'



todo
1. cutting: visualize the starting points
2. remove “select object” or “select face” mode for now (maybe comment out don’t delete). For now we assume there is only one object to work with, we directly select a face, if no face is selected, the object is  always “selected”.