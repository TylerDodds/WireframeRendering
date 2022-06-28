#  Wireframe Rendering Overview

![Wireframe Rendering In Action](wireframe-example.gif)

The Wireframe Rendering Package performs setup and rendering of object wireframes.

It draws the sharp boundary edges of objects, instead of internal edges defined
by the triangles of the underlying 3D model.

Two components are employed to perform the wireframe rendering:

* A Renderer Feature designed for use with the
Universal Render Pipeline (URP) Renderer.
* Additional information on mesh import. Choices include the following:
  * `Texture Coordinates`: Generated wireframe uv values that distinguish which edges should have
wireframes drawn, done on model import.
  * `Geometry Shader`: Generated mesh with line topology for use with a geometry shader.

## Requirements

Requires a project using the Universal Render Pipeline.
Tested on Unity 2020.3 and URP 10.8.1.

## Getting started

All settings and options can be found under `Tools > Wireframe Rendering`.
`Settings` contains project and import settings. Most importantly, it has the
`Wireframe Method` setting, with choices of `Texture Coordinates` or
`Geometry Shader`. In general, the `Geometry Shader method` is more flexible,
but is not supported on all devices. This is the `Default` option if the graphics
API supports geometry shaders.

Wireframe information will be automatically generated for any models
imported in folders ending with `_Wireframe`
(this suffix is configurable in the settings).

Select your Universal Render Pipeline Asset
(can be found in `Project Settings > Quality`) to see the list of Renderers.
Often, this will be a single Forward renderer.

Select a Renderer, click `Add Renderer Feature` and choose
`Wireframe Rendering Feature`. Choose the Layer Mask of objects you want to
have wireframe edges added to, and change the look of the edges.
The URP asset may need a particular set up depending on the features chosen,
so follow any set-up warnings.

Ensure that your models have logically-distinct edges, whether through sharp
edges or seams designated in your modeling software.

## Wireframe Information

### Generating Wireframe Information

Beyond automatic generation based on folder name, this package uses
the `userData` field of the model's `.meta` file to hold information about
generation of wireframe information.
This information is uv coordinates when using the `Texture Coordinates` method,
and line topology mesh when using the `Geometry Shader` method.
This can be set in the Wireframe Settings; see the following subsections for more details.

There are three relation options under `Tools > Wireframe Rendering`:

| Option | Effect |
| ------ | ------ |
| `Clear Wireframe Generation Information For Selected Models` | Removes any information in the `userData` field, falling back to default behaviour based on folder name. |
| `Generate Wireframe Information For Selected Models` | Adds information in `userData` field indicating that wireframe information should be generated, regardless of folder. |
| `Do Not Generate Wireframe Information For Selected Models` | Adds information in `userData` field indicating that wireframe information should not be generated, regardless of folder. |

The `Default` method will choose `Geometry Shader` when the graphics API supports
geometry shaders.

### Wireframe Information (`Texture Coordinates` Method)

An additional set of texture coordinates is generated in a given UV channel.
These four floating-point coordinates per vertex, when interpolated on each
triangle of the mesh, identify wireframe edges.

This means that the mesh's triangular structure is left unchanged, and can be
rendered as usual in the non-wireframe fashion,
ensuring that shaders don't use the UV channel set aside for
the wireframe coordinates.

These triangles are rendered again by the wireframe rendering feature
(twice, if rendering both in-front and behind).

### Wireframe Information (`Geometry Shader` Method)

An additional mesh containing only the required line segments for the wireframe
edges is generated. These segments then rendered by a geometry shader, turning
each segment into a line with the appropriate thickness.

Since the initial model needs to be rendered as usual with all its triangles,
an additional mesh must be created. To facilitate this, a new file of type
`.wiremesh` is generated as additional mesh information, containing the line
segments mesh and generating a prefab with both the regular mesh and the
line segment mesh.

These line segment meshes should not be rendered by the URP Renderer, so remove the
wireframe layer from the Opaque and Transparent Layer Masks in the URP Asset.
The line segment meshes will rendered by the wireframe rendering feature using
a geometry shader (twice, if rendering both in-front and behind).

## Wireframe Settings

Settings can be accessed under `Tools > Wireframe Rendering > Open Settings`
or through the Project Settings Window under title Wireframe Rendering.

### General Settings
| Setting | Effect |
| ------- | ------ |
Reimport Models | Reimports all models in the project. Although most settings changes will prompt to reimport the relevant models, you can use this to ensure all models are imported with up-to-date settings.
Wireframe Method | The method for generating wireframe information on mesh import and rendering the resulting wireframe edges. See the Wireframe Information section for more details.
Directory Suffix | Models found in folders whose name end in this suffix will have wireframe uvs automatically generated.
User Configurable | Whether user configuration menu items are available. When the `userData` portion of model `.meta` files is needed by other packages or assets, disabling this option will not store any information in `userData`, so wireframe information generation will only happen automatically based on folder name.
Wireframe Information Generation Status in Project Window | Enable this to add additional text showing the status of wireframe information coordinate generation (enabled, disabled, or automatically generated).

### `Texture Coordinate` Method
| Setting | Effect |
| ------- | ------ |
UV Channel | Which UV channel (from 0 through 7) the wireframe uv coordinates should be stored in. Be careful to choose a large enough value that other uv Coordinates  will not be overwritten. Default is 3, since that is the largest channel currently accessible by    ShaderGraph.
Sharp Edge Angle | Wireframe uv generation needs to be handled differently when two edges are at different enough angles to be considered not continuously connected. Generally, try to keep this as high as possible.
Do not Weld Vertices | Model import settings may have `Weld Vertices` turned on by default. Enable this option to turn off this option for imported models that have wireframe uv coordinates generated. Turning off `Weld Vertices` may prevent undesired combining of physically-identical vertices that are represented as different vertices of the model, which occurs, for example, with split sharp edges or seams.

### `Geometry Shader` Method
| Segment Generation Setting | Effect |
| ------- | ------ |
Import Object Normals | Encodes object-space segment normals for potential use when rendering world-space segments.
Import Contour Edges | Imports _all_ edges of a model and encodes neighbouring face normals, so that contour edges can be rendered.

Note that the options above require specifying additional information per segment that
prevents combining vertices and edges of adjacent faces, resulting in imported meshes
with a higher number of vertices and segments.
This will impact performance somewhat.

| Importer Setting Setting | Effect |
| ------- | ------ |
Wireframe Layer | Sets the Layer for wireframe GameObjects created by wireframe mesh Importers (for generated `.wiremesh` file types).
Default Weld Distance | Sets the default vertex weld distance for wireframe mesh Importers (for generated `.wiremesh` file types).


## Wireframe Renderer Feature Settings

Settings are visible in the Inspector of the Renderer Feature.
`Tools > Wireframe Rendering > Select Renderer Feature` will select the first
Wireframe Rendering Feature found in Renderers of the  URP asset for the current
Quality setting.

### General Information

| Setting | Effect |
| ------- | ------ |
Wireframe Type | The type of wireframe rendering, as chosen in the Settings.
Layer Mask | The LayerMask for objects that will have wireframe edges drawn.
UV Channel (`Texture Coordinates`) | Shows the UV channel being used for the wireframe texture coordinates,as chosen in the Settings.
Object Normals Imported (`Geometry Shader`) | Shows if object normals are imported, as chosen in the Settings.

### General Settings

| Setting | Effect |
| ------- | ------ |
In Front Wireframe | If wireframe should be drawn for edges visible to the camera (in front of other objects).
In Behind Wireframe | If wireframe should be drawn for edges found behind other surfaces.
Behind Looks Like In-Front | If wireframe for in-behind edges should have the same style settings as the in-front edges.

### Line Style Settings

Line Settings can be set separately for in-front and in-behind lines.

| Setting | Effect |
| ------- | ------ |
Color | Color of the line. Drawn with alpha blending.
Width | Thickness of the line in pixels or world-space. Units differ depending on `World Space` setting.
Falloff Width | Distance in pixels that the line fades out over.
Overshoot (`Geometry Shader` Method) | If lines should be drawn to overshoot the line segment endpoints.
&nbsp;&nbsp; Overshoot Units | The length of the overshoot (in pixels or world-space). Units differ depending on `World Space` setting.
Dash | Whether the line should be drawn dashed.
&nbsp;&nbsp; Dash Length | The length of each dash (in pixels or world-space). Units differ depending on `World Space` setting.
&nbsp;&nbsp; Empty Length | The length of space between each dash (in pixels or world-space). Units differ depending on `World Space` setting.
Apply Texture | If a texture should be applied to the line.
&nbsp;&nbsp; Texture | The texture to apply. Only r channel is used.
&nbsp;&nbsp; Keep Texture Aspect Ratio | If the texture's aspect ratio should be respected to determine the length of the texture.
&nbsp;&nbsp; Texture Length | The length of the texture, if aspect ratio is not kept fixed. Units differ depending on `World Space` setting.
Show Contour Edges (`Geometry Shader` Method) | Shows contour edges of the model in wireframe. Contour edges are shared by a pair of triangles pointing opposite directions from the camera position. Requires contour edge information to be imported.
World Space | Determines whether width and dash length are set in pixels or world-space units (m).
&nbsp;&nbsp; Use Object Normals (`Geometry Shader` Method) | When `World Space`, aligns segments based on object normal vectors calculated from neighbouring faces, instead of based on screen position. Requires object normals to be imported.
Fresnel (`Texture Coordinates` Method) | If edges should be drawn based on if an edge is oriented perpendicular to the view direction.

### Depth Fade Settings

Depth fade settings only apply to in-behind lines.

| Setting | Effect |
| ------- | ------ |
Fade With Depth | If depth of in-behind edge should be used to fade out and thin the wireframe line. Requires Camera Depth Texture to be enabled.
&nbsp;&nbsp; Depth Fade Distance | Distance scale used for depth fading (m).

### Haloing Settings

Haloing is only available when both in-front and in-behind lines are drawn.

With this effect, in-behind wireframes are not drawn at a given distance from in-front lines.

| Setting | Effect |
| ------- | ------ |
Haloing | If haloing effect is enabled.
&nbsp;&nbsp; Haloing Width | The distance of haloing effect in pixels from in-front wireframe line. Units differ depending on `World Space` setting if using `Geometry Shader` method.
&nbsp;&nbsp; Stencil reference value (Advanced) | Reference value used by the stencil buffer to determine where in-front lines have been drawn.

### Advanced Settings (`Geometry Shader` method)

Advanced settings to manage depth sampling used by the geometry shader method.

Note that scene depth is sampled around the line width's center to determine
if the line is in front or behind of scene objects. Since this will fail when the
line center is outside of the viewport, the width is tapered as lines leave the
viewport, and the alpha is faded out as well.

In addition to these settings, enabling the `Behind Looks Like In-Front` setting
bypasses the need to use the sampled depth to categorize edges as in-front or
behind surfaces, so it's recommended to enable that setting in cases where it's
stylistically appropriate.

| Setting | Effect |
| ------- | ------ |
In Front Depth Cutoff | The cutoff depth between when pixels are considered for the in-front or behind objects wireframe passes.
Viewport Edge Width Taper Start | Amount of viewport coordinate beyond edges to begin tapering width of the segments. For example, -0.01 will begin tapering width at 1% of the screen size before the edge.
Viewport Edge Width Taper End | Amount of viewport coordinate beyond edges to begin tapering width of the segments. For example, 0.01 will end tapering width at 1% of the screen size beyond the edge.
Viewport Edge Alpha Fade Start | As above, but for fading alpha of the segments.
Viewport Edge Alpha Fade End | As above, but for fading alpha of the segments.

### Warnings and Messages

Additional warnings or messages will be given to ensure that the URP Assets and
Renderers are set up correctly to support the chosen features.

#### Depth Texture

The URP Assets using the Wireframe Rendering Feature need the `Depth Texture`
enabled when using `Geometry Shader` method, or when `Fade With Depth` is enabled
using the `Texture Coordinates` method.

#### Renderer LayerMasks

When using the `Geometry Shader` method, each Renderer using the Wireframe
Rendering Feature should disable the wireframe Layer in the `Opaque Layer Mask`
and `Transparent Layer Mask` settings, so that the wireframe meshes are only
drawn by the feature. Note that the initial model is still rendered separately,
as normal.

## Models and Importing

This style of triangle-based edge rendering works best on straight edges.
Neighbouring edges with angles between them may show slight artifacts.

The underlying triangle structure of the mesh will affect how edges are drawn,
particularly other triangles sharing a vertex with two adjacent wireframe edges.
Experimenting with placement and topology of triangles can help to yield better
results, particularly for non-straight edges or for Fresnel calculations.

Wireframe edges are defined to belong to only one triangle. In practical terms,
this means that vertices of these edges must be considered logically distinct,
whether split through sharp angle / edge splitting, uv or other seams, etc.
This may also require ensure at least one subdivision of each surface of the
model.

Many of the geometry-related properties of the
[model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html)
will affect whether vertices will be logically split on import.
These include `Normals`, `Normals Mode`, `Smoothness Source`, `Smoothing Angle`,
and `Tangents`. For low-poly style models, a good starting point might be to
choose to `Import` normals with mode `Unweighted`, a smoothness source of `None`
and `Calculate Mikktspace` tangents.

## Caveats

### Material Setup

In most cases, objects need to be rendered normally
(in addition to by the wireframe feature) so that the appropriate depth of objects
is determined. This may differ from object to object, so materials should be
set as usual through the Editor.
Any URP shader with Opaque surface type will work; Unlit is a good starting point stylistically.

Note that Transparent materials will not write depth information like opaque ones,
so these are not good choices if you want these surfaces to obscure the wireframe
rendering.

> Note that custom Depth Mask or other depth-only shaders do not integrate well
with URP. First, they cause rendering issues when rendering Skyboxes, since
the skybox is rendered after opaque objects, so uninitialized results will be shown.
Second, any such shaders would need a `DepthOnly` pass so that they can be included
in writing the URP Depth Texture, which is needed in several cases, as discussed above.

### Texture coordinate Generation

In some edge conditions (long boundary edge groups that contain many sharp
angles, triangles containing two boundary edges, or triangles whose vertices
all touch a boundary edge) calculation may be prohibitively expensive
when importing for the `Texture Coordinates` method.
