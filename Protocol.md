# TCP Protocol

Information will be exchanged by communication between two classes in corresponding applications: [Server Controller](Server/Assets/Scripts/ServerController.cs) and [Client Controller](Client/Assets/Scripts/ClientController.cs). Except the roles of which they establish connection, the ways they send messages are the same.

Since tcp package has limited capacity and long messages will be broken into several packages, an extra character `?` marks the beginning of a message and `!` marks the end. In `void Update()` a simple while loop is used to reconstruct whole message from received packages. `void getVector()` is for parsing information from the message, which is in `string` type upon received.

`string[msgTypes] sendBuffer` is used as a buffer to control the speed of message sending. New information will be first stored in corresponding cells in `sendBuffer` (Overwrite previous information if it hasn't been sent) through `void sendMessage(string msg)`. Every `sendInterval` second `void sendMsgInBuffer()` will send out everything in `sendBuffer` and empty it.

Note that since `void getVector()` has limited speed of mesasge parsing, `sendInterval` cannot be too short, otherwise increasing lagging may appear because of unprocessed message piling up.


## Following is the message format for all types of information:

### From server to client:

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
- Second line -1 or select face index
- Third line -1 or snapped face index

Face Track:
- First line "Face"
- Second Line Vector3 or '0' for ortho

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

"Snap"

"Enable cutting plane"

"Execute cutting plane"

Resend mesh
- "RM"

Resend transform
- "RT"