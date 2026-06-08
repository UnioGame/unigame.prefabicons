# Prefab Icon Renderer

A tool for generating icons from 3D Unity prefabs with support for backgrounds, frames, and full render parameter customization.

## Features

- **Prefab folder sources** - Select one or more asset folders and discover prefabs automatically
- **Prefab dropdown** - Pick a prefab from the discovered folder contents without dragging it manually
- **Auto naming** - Generate names from prefab name and icon resolution
- **Collision policy** - Choose whether existing files get `_N` suffixes or are overwritten
- **Batch processing** - Render all discovered prefabs with the same render settings
- **Full customization** - Backgrounds, frames, tints, and transformations
- **Public API** - Use from EditorWindow or directly from code

## Usage Through UI

1. Open window: **UniGame/Tools → Render Prefab Icon**
2. Add prefab source folders in **Prefab Sources**:
   - Click **Add Folder** after selecting a folder asset.
   - Click **Refresh Prefabs** after changing folder contents.
   - Use **Prefab From Sources** to select one discovered prefab.
3. Optionally select a prefab manually in **Prefab**. Manual selection still works even if no source folders are configured.
4. Configure output:
   - **Default Save Folder** - Folder where PNG sprites are written.
   - **Auto Naming** - When enabled, output names use `{PrefabName}_{Resolution}`.
   - **Name Collision Policy**:
     - `AppendNumber` creates `Name.png`, then `Name_1.png`, `Name_2.png`, and so on.
     - `Overwrite` writes to the same `Name.png`, which is useful for regenerating a whole icon set.
   - **Icon Filename** - Used only when Auto Naming is disabled.
5. Configure render parameters:
   - **Resolution** - Icon size (128-1024)
   - **Rotation** - Object rotation
   - **Camera Zoom** - Camera zoom level
   - **Prefab Zoom** - Prefab scale
   - **Prefab Offset** - X/Y axis offset

6. Expand **Advanced Layer Settings** to configure layers:
   - **Background** - Background sprite
   - **Frame** - Frame around the icon
   - Tints for each layer

7. Click **Render & Save Icon** for a single icon or **Render All Found Prefabs** for batch rendering.

### Batch Rendering Notes

- Batch rendering uses the prefabs discovered from **Prefab Sources**.
- Batch rendering requires **Auto Naming** to be enabled.
- All prefabs in the batch use the same render settings, save folder, background, frame, rotation, zoom, and offset.
- Use `Overwrite` when you want to regenerate an existing icon set in place.
- Use `AppendNumber` when you want to preserve existing files and create new variants.

## Code API Usage

### Simple Render and Save

```csharp
using PrefabIconRenderer;
using UnityEditor;

var settings = new PrefabIconSettings
{
    prefab = myPrefab,
    resolution = 512,
    fileName = "MyIcon",
    folderPath = "Assets/GeneratedSprites"
};

var engine = new IconRendererEngine(settings);
var result = engine.RenderAndSave();

if (result.Success)
{
    Debug.Log($"Saved: {result.SavedPath}");
}
```

### Render to Memory

```csharp
var settings = new PrefabIconSettings
{
    prefab = myPrefab,
    resolution = 256,
    objectRotation = new Vector3(20f, 45f, 0f)
};

var engine = new IconRendererEngine(settings);
var result = engine.RenderToTexture();

if (result.Success)
{
    // Use texture2D
    myImage.texture = result.Texture;
}
```

### Render Specific GameObject

```csharp
var engine = new IconRendererEngine();
engine.settings.resolution = 512;
engine.settings.cameraZoom = 5f;

// Render to texture
var result = engine.RenderGameObjectToTexture(myGameObject);
if (result.Success)
    Debug.Log("Icon ready");

// Render and save
var savedResult = engine.RenderGameObjectAndSave(myGameObject, "custom_icon", "Assets/Icons");
```

### Full Customization with Layers

```csharp
var settings = new PrefabIconSettings
{
    prefab = myPrefab,
    resolution = 512,
    transparentBackground = false,
    backgroundColor = Color.white,
    
    // Transform
    objectRotation = new Vector3(25f, -30f, 0f),
    cameraZoom = 2f,
    prefabZoom = 1.2f,
    prefabOffset = new Vector2(0.1f, -0.05f),
    
    // Background
    backgroundSprite = bgSprite,
    backgroundZoom = 1f,
    tintBackground = true,
    backgroundTintColor = new Color(0.8f, 0.8f, 1f),
    
    // Frame
    frameSprite = frameSprite,
    frameZoom = 1.1f,
    tintFrame = false,
    
    fileName = "AdvancedIcon",
    folderPath = "Assets/GeneratedSprites"
};

var engine = new IconRendererEngine(settings);
var result = engine.RenderAndSave();
```

### Batch Processing

```csharp
foreach (var prefab in prefabsToRender)
{
    var settings = new PrefabIconSettings
    {
        prefab = prefab,
        resolution = 512,
        fileName = IconRendererEngine.CreateAutoFileName(prefab, 512),
        folderPath = "Assets/GeneratedSprites",
        nameCollisionPolicy = PrefabIconNameCollisionPolicy.AppendNumber
    };
    
    var engine = new IconRendererEngine(settings);
    var result = engine.RenderAndSave();
}
```

### Save With Overwrite Policy

```csharp
var settings = new PrefabIconSettings
{
    prefab = myPrefab,
    resolution = 512,
    folderPath = "Assets/GeneratedSprites",
    nameCollisionPolicy = PrefabIconNameCollisionPolicy.Overwrite
};

var engine = new IconRendererEngine(settings);
var fileName = IconRendererEngine.CreateAutoFileName(myPrefab, settings.resolution);
var result = engine.RenderAndSave(fileName, settings.nameCollisionPolicy);
```

## PrefabIconSettings Parameters

### Core
- `prefab` - Prefab to render
- `resolution` - Icon size (128-1024, default: 512)
- `fileName` - Output file name when auto naming is disabled (default: "NewIcon")
- `folderPath` - Save folder path (default: "Assets/GeneratedSprites")
- `autoNaming` - Whether UI output names are generated as `{PrefabName}_{Resolution}` (default: true)
- `nameCollisionPolicy` - Save behavior when target PNG already exists (default: `AppendNumber`)

### Background
- `transparentBackground` - Transparent background (default: true)
- `backgroundColor` - Background color (default: white)

### Transform
- `objectRotation` - Object rotation in Euler degrees (default: Vector3.zero)
- `cameraZoom` - Camera zoom level (default: 2)
- `prefabZoom` - Prefab scale (default: 1)
- `prefabOffset` - X/Y offset (default: Vector2.zero)

### Background Layer
- `backgroundSprite` - Background sprite
- `backgroundZoom` - Background scale (default: 1)
- `backgroundOffset` - Background offset (default: Vector2.zero)
- `tintBackground` - Tint background (default: false)
- `backgroundTintColor` - Background tint color (default: white)

### Frame Layer
- `frameSprite` - Frame sprite
- `frameZoom` - Frame scale (default: 1)
- `frameOffset` - Frame offset (default: Vector2.zero)
- `tintFrame` - Tint frame (default: false)
- `frameTintColor` - Frame tint color (default: white)

### Name Collision Policy

- `PrefabIconNameCollisionPolicy.AppendNumber` - Keep existing files and append `_1`, `_2`, etc.
- `PrefabIconNameCollisionPolicy.Overwrite` - Reuse the same output path and replace the existing PNG.

## API Methods

### IconRendererEngine

- `RenderToTexture()` - Render prefab to texture in memory
- `RenderAndSave()` - Render prefab and save to PNG file
- `RenderAndSave(string fileName, PrefabIconNameCollisionPolicy collisionPolicy, ISet<string> reservedPaths = null)` - Render and save with an explicit output name and collision policy
- `RenderGameObjectToTexture(GameObject)` - Render any GameObject to texture
- `RenderGameObjectAndSave(GameObject, string fileName, string folderPath)` - Render any GameObject and save
- `CreateAutoFileName(GameObject, int resolution)` - Build the default auto name `{PrefabName}_{Resolution}`
- `ResolveIconPath(string fileName, PrefabIconNameCollisionPolicy collisionPolicy, ISet<string> reservedPaths = null)` - Resolve the final PNG path before saving
- `UpdateSettings(PrefabIconSettings)` - Update engine settings
- `GetSettings()` - Get a copy of current settings

### PrefabIconRenderResult

- `Success` - Whether render succeeded
- `Texture` - Rendered texture (if successful)
- `SavedPath` - Path to saved file (if saved)
- `ErrorMessage` - Error message (if failed)

## License

MIT
