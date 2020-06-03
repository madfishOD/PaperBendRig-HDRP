## Hybrid procedural rig for paper sheet bending

## Normal Calculation
Parts of shader responsible for deformed mesh normals recalculation taken from here(https://gamedevbill.com/shader-graph-normal-calculation/).

* SubGraphNormalCorrection - same logic as FullNormalCorrection but broken into sub-graphs. Useful to model when actually doing this in real life as it's a lot easier to hook up.
* Neighbors - first stage subgraph. Used by SubGraphNormalCorrection before feeding into the vertex displacement.
* NewNormal - final stage subgraph. Used by SubGraphNormalCorrection after feeding into the vertex displacement.
