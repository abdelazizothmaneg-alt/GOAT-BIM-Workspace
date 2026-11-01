---
name: blender-archviz-automation
description: Automates Blender architectural visualization via Python (bpy) and BlenderMCP, configuring real-world scales, two-point perspective shift cameras, Nishita physical sky daylight, IES photometric downlights, and Cycles PBR lookdev.
---

# Blender ArchViz Automation

Master-level architectural visualization and automation skill for Blender using Python (`bpy`) and the Model Context Protocol (BlenderMCP). Enforces architectural scale accuracy, two-point perspective vertical lines, realistic sun/sky models, and photorealistic Cycles rendering.

---

## 1. Architectural Camera & Optics in Blender (`bpy`)

In professional architectural photography, camera pitch (tilt around the X-axis) must be eliminated ($0.0^\circ$) to maintain vertical lines. Instead, use Blender's **Camera Shift Y** property:

```python
import bpy

def setup_archviz_camera(name="ArchViz_Cam", location=(5.0, -8.0, 1.5), target_z=1.5, focal_length=28.0, shift_y=0.2):
    """
    Creates a two-point perspective architectural camera.
    Camera stays level (0 deg pitch) to ensure straight verticals.
    """
    cam_data = bpy.data.cameras.new(name)
    cam_data.lens = focal_length
    cam_data.shift_y = shift_y # Shift upward to capture tall buildings without tilting!
    cam_data.clip_start = 0.1
    cam_data.clip_end = 1000.0
    
    cam_obj = bpy.data.objects.new(name, cam_data)
    bpy.context.collection.objects.link(cam_obj)
    
    cam_obj.location = location
    # Pitch is locked at 90 deg from top (meaning horizontal look)
    cam_obj.rotation_euler = (1.570796, 0.0, 0.58) # (90 deg, 0 deg roll, yaw)
    
    bpy.context.scene.camera = cam_obj
    return cam_obj
```

---

## 2. Physical Daylight Setup: Nishita Sky Texture

Blender's **Nishita Sky** simulates physical Rayleigh and Mie scattering, ozone absorption, and realistic solar altitude/azimuth:

```python
def setup_nishita_sky(sun_elevation_deg=35.0, sun_rotation_deg=135.0, air_density=1.0, dust=1.2, ozone=1.0):
    world = bpy.context.scene.world
    world.use_nodes = True
    nodes = world.node_tree.nodes
    links = world.node_tree.links
    nodes.clear()
    
    # Nodes
    node_out = nodes.new(type="ShaderNodeOutputWorld")
    node_bg = nodes.new(type="ShaderNodeBackground")
    node_sky = nodes.new(type="ShaderNodeTexSky")
    
    node_sky.sky_type = 'NISHITA'
    node_sky.sun_elevation = math.radians(sun_elevation_deg)
    node_sky.sun_rotation = math.radians(sun_rotation_deg)
    node_sky.air_density = air_density
    node_sky.dust_density = dust
    node_sky.ozone_density = ozone
    node_sky.sun_disc = True
    node_sky.sun_size = math.radians(0.545) # Real angular diameter of the sun
    
    # Connect
    links.new(node_sky.outputs["Color"], node_bg.inputs["Color"])
    links.new(node_bg.outputs["Background"], node_out.inputs["Surface"])
    node_bg.inputs["Strength"].default_value = 1.0
```

---

## 3. Interior Photometric Lights & IES Profiles

For downlights and recessed spots:
- Set Point or Area light with **Use Nodes = True**.
- Connect a **ShaderNodeTexIES** to the Emission strength socket.
- Adjust color temperature using **ShaderNodeBlackbody** (e.g. $2700\text{K}$ for warm luxury hospitality, $4000\text{K}$ for commercial offices).

---

## 4. Production Cycles Render Configuration

```python
def configure_cycles_production():
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    cycles = scene.cycles
    
    # GPU Compute
    cycles.device = 'GPU'
    
    # Noise Threshold & Samples
    cycles.use_adaptive_sampling = True
    cycles.adaptive_threshold = 0.01
    cycles.samples = 1024
    
    # Light Paths (Architectural Interior Quality)
    cycles.max_bounces = 12
    cycles.diffuse_bounces = 4
    cycles.glossy_bounces = 4
    cycles.transmission_bounces = 8 # Crisp transparent glass
    cycles.transparent_max_bounces = 16
    cycles.caustics_reflective = False # Prevent fireflies
    cycles.caustics_refractive = True
    
    # Denoising
    cycles.use_denoising = True
    cycles.denoiser = 'OPENIMAGEDENOISE'
    cycles.denoising_prefilter = 'ACCURATE'
```

---

## 5. Deliverables & BlenderMCP Integration Checklist
1. **Live BlenderMCP Session**: Real-time scene execution via BlenderMCP addon inside Blender's N-Panel.
2. **Collection Organization**: Strict segregation: `01_Architecture`, `02_Openings_Glass`, `03_Furniture_FFNE`, `04_Lighting`, `05_Cameras`.
3. **Multi-Pass EXR Output**: Render elements for Compositing (Cryptomatte, Depth, Mist, Ambient Occlusion).
