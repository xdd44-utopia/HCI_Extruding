## Functions?

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
   Input: edges of a single face
   Return: Corresponding boundaries (`List<List<List<int>>> boundaries[#face][#boundary][#vertex]`)

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
   Input: face vertices (`Vector3[]`), face triangles (`int[]`)
   Return: boundaries (first outer, then holes) (`List<List<int>> [#boundary][#vertex]`)
1. Simplify mesh?
   Input: vertices, triangles
   Return: vertices, triangles
   - Remove duplicated vertices
   - Recreate triangularization?
   - Calculate edges

2. Simplify mesh

### Vector calculator