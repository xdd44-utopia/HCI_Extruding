## Functions?

`const float eps = 0.0000001`

### Object

If geometry changed (when adding or removing vertices):
  
1. Simplify mesh
   Input: vertices, triangles
   Return: simplified vertices, remapped triangles

2. Extract edges
   Input: vertices, triangles
   Return: edges (`int edges[#edge * 2 / #edge * 2 + 1]` pointing to #vertex), edges for each triangles (`int triangleEdges[#triangle]` pointing to #edge)

3. Categorize face
   Input: vertices, triangles
   Return: Categorized #triangle index by faces (`List<List<int>> faces[#triangles]`)

4. Extract boundaries
   Use `MeshCalculator.extractBoundary()`
   Result: Corresponding boundaries (`List<List<List<int>>> boundaries[#face][#boundary][#vertex]`)

If anything changed:

5. Generate covers
   Input: Mesh
   Result: Cover face (`Gameobjects`)

6. Send mesh
   
7. Synchronize inside object

Highlight face
   Input #triangle index
   - Select/deselect
   - Change/remove color
   - PROBLEM: Highlight may not be preserved when geometry changes

### Mesh calculator

1. Extract boundary
   Input: vertices, triangles, triangleEdges, edges, for a single planar face.
   Return: boundary for the face (`List<List<int>> boundaries[#boundary][#vertice]`, first for outline, then holes if exist)

2. Extract edges
   Input: vertices, triangles
   Return: edges (`int edges[#edge * 2 / #edge * 2 + 1]` pointing to #vertex), edges for each triangles (`int triangleEdges[#triangle]` pointing to #edge)

3. Simplify mesh
   Input: vertices, triangles
   Return: vertices, triangles
   - Remove duplicated & unused vertices
   - Remap triangles
   - Recreate triangularization?

4. Generate cover mesh
   Input: vertices, boundaries, faceTriangles (of a single face)
   Return: faceVertices, faceTriangles
   - Offset in normal direction
   - Offset to fake boundaries

### Vector calculator

1. areLinesIntersect
   Input: two end vertices for two lines.
   Return: bool

2. Cross product
   
3. Dot product

4. Vector mean

5. Vector variance