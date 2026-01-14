# Prefab Icon Renderer

A tool for generating icons from 3D Unity prefabs with support for backgrounds, frames, and full render parameter customization.

## Features

- 🚀 **Batch Processing** - Render multiple icons simultaneously
- 🎨 **Full Customization** - Backgrounds, frames, tints, and transformations
- 📦 **Public API** - Use from EditorWindow or directly from code

## Usage Through UI

1. Open window: **UniGame/Tools → Render Prefab Icon**
2. Select a prefab
3. Configure parameters:
   - **Resolution** - Icon size (128-1024)
   - **Rotation** - Object rotation
   - **Camera Zoom** - Camera zoom level
   - **Prefab Zoom** - Prefab scale
   - **Prefab Offset** - X/Y axis offset

4. Expand **Advanced Layer Settings** to configure layers:
   - **Background** - Background sprite
   - **Frame** - Frame around the icon
   - Tints for each layer

5. Click **Render & Save Icon**

## Code API Usage

### Simple Render and Save

```csharp
using PrefabIconRenderer;
using UnityEditor;

var settings = new PrefabIconSettings
{
    Prefab = myPrefab,
    Resolution = 512,
    FileName = "MyIcon",
    FolderPath = "Assets/GeneratedSprites"
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
    Prefab = myPrefab,
    Resolution = 256,
    ObjectRotation = new Vector3(20f, 45f, 0f)
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
engine.settings.Resolution = 512;
engine.settings.CameraZoom = 5f;

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
    Prefab = myPrefab,
    Resolution = 512,
    TransparentBackground = false,
    BackgroundColor = Color.white,
    
    // Transform
    ObjectRotation = new Vector3(25f, -30f, 0f),
    CameraZoom = 2f,
    PrefabZoom = 1.2f,
    PrefabOffset = new Vector2(0.1f, -0.05f),
    
    // Background
    BackgroundSprite = bgSprite,
    BackgroundZoom = 1f,
    TintBackground = true,
    BackgroundTintColor = new Color(0.8f, 0.8f, 1f),
    
    // Frame
    FrameSprite = frameSprite,
    FrameZoom = 1.1f,
    TintFrame = false,
    
    FileName = "AdvancedIcon",
    FolderPath = "Assets/GeneratedSprites"
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
        Prefab = prefab,
        Resolution = 512,
        FileName = prefab.name + "_Icon",
        FolderPath = "Assets/GeneratedSprites"
    };
    
    var engine = new IconRendererEngine(settings);
    var result = engine.RenderAndSave();
}
```

## PrefabIconSettings Parameters

### Core
- `Prefab` - Prefab to render
- `Resolution` - Icon size (128-1024, default: 512)
- `FileName` - Output file name (default: "NewIcon")
- `FolderPath` - Save folder path (default: "Assets/GeneratedSprites")

### Background
- `TransparentBackground` - Transparent background (default: true)
- `BackgroundColor` - Background color (default: white)

### Transform
- `ObjectRotation` - Object rotation in Euler degrees (default: Vector3(25, -30, 0))
- `CameraZoom` - Camera zoom level (default: 2)
- `PrefabZoom` - Prefab scale (default: 1)
- `PrefabOffset` - X/Y offset (default: Vector2.zero)

### Background Layer
- `BackgroundSprite` - Background sprite
- `BackgroundZoom` - Background scale (default: 1)
- `BackgroundOffset` - Background offset (default: Vector2.zero)
- `TintBackground` - Tint background (default: false)
- `BackgroundTintColor` - Background tint color (default: white)

### Frame Layer
- `FrameSprite` - Frame sprite
- `FrameZoom` - Frame scale (default: 1)
- `FrameOffset` - Frame offset (default: Vector2.zero)
- `TintFrame` - Tint frame (default: false)
- `FrameTintColor` - Frame tint color (default: white)

## API Methods

### IconRendererEngine

- `RenderToTexture()` - Render prefab to texture in memory
- `RenderAndSave()` - Render prefab and save to PNG file
- `RenderGameObjectToTexture(GameObject)` - Render any GameObject to texture
- `RenderGameObjectAndSave(GameObject, string fileName, string folderPath)` - Render any GameObject and save
- `UpdateSettings(PrefabIconSettings)` - Update engine settings
- `GetSettings()` - Get a copy of current settings

### PrefabIconRenderResult

- `Success` - Whether render succeeded
- `Texture` - Rendered texture (if successful)
- `SavedPath` - Path to saved file (if saved)
- `ErrorMessage` - Error message (if failed)

## License

MIT
