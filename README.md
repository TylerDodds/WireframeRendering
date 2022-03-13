#  Wireframe Rendering Overview

![Wireframe Rendering In Action](Documentation~/wireframe-example.gif)

## Overview

The Wireframe Rendering Package performs setup and rendering of object wireframes.

It draws the sharp boundary edges of objects, instead of internal edges defined
by the triangles of the underlying 3D model.

Two components are employed to perform the wireframe rendering:

* A Renderer Feature designed for use with the
Universal Render Pipeline (URP) Renderer.
* Generated wireframe uv values that distinguish which edges should have
wireframes drawn, done on model import.

## Installation instructions

See the [package manager installation instructions](https://docs.unity3d.com/Manual/upm-ui-install.html).

## Requirements

Requires using the Universal Render Pipeline.
Tested on Unity 2020.3 and URP 10.8.1.

## Workflows

All settings and options can be found under `Tools > Wireframe Rendering`.

Wireframe uv coordinates will be automatically generated for any models
imported in folders ending with `_Wireframe`
(this suffix is configurable in the settings).

Select your Universal Render Pipeline Asset
(can be found in `Project Settings > Quality`) to see the list of Renderers.
Often, this will be a single Forward renderer.

Select a Renderer, click `Add Renderer Feature` and choose
`Wireframe Rendering Feature`. Choose the Layer Mask of objects you want to
have wireframe edges added to.

Ensure that your models have logically-distinct edges, whether through sharp
edges or seams designated in your modeling software.

## Reference

See the [Markdown documentation](Documentation~/wireframe-rendering.md) for further details.
