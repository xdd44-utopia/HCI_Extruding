## 3D Modeling demo of a research in application of non-flat screen

By Unity

### TCP Message
Mesh (From server to client):
- First line "Mesh"
- Second Line $n$ for $n$th mesh
- Third line $nv$ for $nv$ vertices
- Followed by one line of vertices separated by comma
- Fourth line $nt$ for $nt$ triangle indices
- Followed by one line of indices separated by comma

Transform (From server to client):
- First line "Transform"
- Second Line $n$ for $n$th objects
- Third line position
- Fourth line rotation quaternion
- Fifth line scale

Highlight (From server to client):
- First line "Highlight"
- Second line "Object" or "Face"
- Third line $n$ for nth object OR $nv$ for $nv$ vertices
- Followed by one line of vertices separated by comma
- Fourth line $nt$ for $nt$ triangle indices
- Followed by one line of indices separated by comma

Face Track (From server to client):
- First line "Face"
- Second Line Vector3

Touching (From client to server):
- First line "Touch"
- Second line n
- Next n line Vector3 position + previous position

Acceleration (From client to server):
- First line "Acc"
- Second line acceleration

Cutting (From client to server):
- First line "Cutting"
- Second line touch point in server's space

Extruding (From client to server):
- First line "Extruding"
- Second line extruding distance






Snap to another screen

Show the angle

Ways to close grid