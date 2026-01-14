using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace PrefabIconRenderer
{
    public class PrefabIconRendererWindow : EditorWindow
    {
        private const string WindowTitle = "Render Prefab Icon";
        private const string DefaultFileNameKey = "PrefabIconRenderer_DefaultFileName";
        private const string DefaultFolderPathKey = "PrefabIconRenderer_DefaultFolderPath";

        private IconRendererEngine engine;
        private PrefabIconSettings settings;

        private ObjectField prefabField;
        private IntegerField resolutionField;
        private TextField fileNameField;
        private TextField defaultFileNameField;
        private TextField folderPathField;
        private Toggle transparentBackgroundToggle;
        private ColorField backgroundColorField;

        private Vector3Field rotationField;
        private FloatField cameraZoomField;

        private FloatField prefabZoomField;
        private Vector2Field prefabOffsetField;

        private ObjectField backgroundSpriteField;
        private FloatField backgroundZoomField;
        private Vector2Field backgroundOffsetField;
        private Toggle tintBackgroundToggle;
        private ColorField backgroundTintColorField;

        private ObjectField frameSpriteField;
        private FloatField frameZoomField;
        private Vector2Field frameOffsetField;
        private Toggle tintFrameToggle;
        private ColorField frameTintColorField;

        private Image previewImage;
        private Label statusLabel;

        [MenuItem("UniGame/Tools/Render Prefab Icon")]
        public static void ShowWindow()
        {
            GetWindow<PrefabIconRendererWindow>(WindowTitle);
        }

        private void OnEnable()
        {
            settings = new PrefabIconSettings();
            engine = new IconRendererEngine(settings);
            
            // Load default values from EditorPrefs
            LoadDefaultSettings();
            
            BuildUI();
            EditorApplication.update += RepaintPreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintPreview;
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            var styleSheet = LoadStyleSheet();
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            var scrollView = new ScrollView();
            scrollView.AddToClassList("scroll-view");

            // Title
            var titleLabel = new Label(WindowTitle);
            titleLabel.AddToClassList("title");
            scrollView.Add(titleLabel);

            // Main Settings Group
            var mainGroup = CreateSettingsGroup("Main Settings");
            scrollView.Add(mainGroup);

            prefabField = new ObjectField("Prefab");
            prefabField.objectType = typeof(GameObject);
            prefabField.RegisterValueChangedCallback(evt =>
            {
                settings.prefab = evt.newValue as GameObject;
                RepaintPreview();
            });
            mainGroup.Add(prefabField);

            resolutionField = new IntegerField("Resolution");
            resolutionField.value = settings.resolution;
            resolutionField.RegisterValueChangedCallback(evt =>
            {
                settings.resolution = Mathf.Clamp(evt.newValue, 128, 1024);
                resolutionField.value = settings.resolution;
            });
            mainGroup.Add(resolutionField);

            fileNameField = new TextField("Icon Filename");
            fileNameField.value = settings.fileName;
            fileNameField.RegisterValueChangedCallback(evt => settings.fileName = evt.newValue);
            mainGroup.Add(fileNameField);

            // Default Settings Group
            var defaultGroup = CreateSettingsGroup("Default Settings");
            scrollView.Add(defaultGroup);

            defaultFileNameField = new TextField("Default Icon Name");
            defaultFileNameField.value = EditorPrefs.GetString(DefaultFileNameKey, "NewIcon");
            defaultFileNameField.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetString(DefaultFileNameKey, evt.newValue);
            });
            defaultGroup.Add(defaultFileNameField);

            folderPathField = new TextField("Default Save Folder");
            folderPathField.value = EditorPrefs.GetString(DefaultFolderPathKey, "Assets/GeneratedSprites");
            folderPathField.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetString(DefaultFolderPathKey, evt.newValue);
            });
            defaultGroup.Add(folderPathField);

            var browseButton = new Button(() => BrowseFolder()) { text = "Browse Folder..." };
            browseButton.AddToClassList("browse-button");
            defaultGroup.Add(browseButton);

            // Background Settings Group
            var backgroundGroup = CreateSettingsGroup("Background");
            scrollView.Add(backgroundGroup);

            transparentBackgroundToggle = new Toggle("Transparent Background");
            transparentBackgroundToggle.value = settings.transparentBackground;
            transparentBackgroundToggle.RegisterValueChangedCallback(evt =>
            {
                settings.transparentBackground = evt.newValue;
                backgroundColorField.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
                RepaintPreview();
            });
            backgroundGroup.Add(transparentBackgroundToggle);

            backgroundColorField = new ColorField("Background Color");
            backgroundColorField.value = settings.backgroundColor;
            backgroundColorField.style.display = settings.transparentBackground ? DisplayStyle.None : DisplayStyle.Flex;
            backgroundColorField.RegisterValueChangedCallback(evt =>
            {
                settings.backgroundColor = evt.newValue;
                RepaintPreview();
            });
            backgroundGroup.Add(backgroundColorField);

            // Transform Settings Group
            var transformGroup = CreateSettingsGroup("Transform");
            scrollView.Add(transformGroup);

            rotationField = new Vector3Field("Rotation (Euler)");
            rotationField.value = settings.objectRotation;
            rotationField.RegisterValueChangedCallback(evt =>
            {
                settings.objectRotation = evt.newValue;
                RepaintPreview();
            });
            transformGroup.Add(rotationField);

            cameraZoomField = new FloatField("Camera Zoom");
            cameraZoomField.value = settings.cameraZoom;
            cameraZoomField.RegisterValueChangedCallback(evt =>
            {
                settings.cameraZoom = Mathf.Clamp(evt.newValue, 0.1f, 10f);
                cameraZoomField.value = settings.cameraZoom;
                RepaintPreview();
            });
            transformGroup.Add(cameraZoomField);

            prefabZoomField = new FloatField("Prefab Zoom");
            prefabZoomField.value = settings.prefabZoom;
            prefabZoomField.RegisterValueChangedCallback(evt =>
            {
                settings.prefabZoom = Mathf.Clamp(evt.newValue, 0.1f, 20f);
                prefabZoomField.value = settings.prefabZoom;
                RepaintPreview();
            });
            transformGroup.Add(prefabZoomField);

            prefabOffsetField = new Vector2Field("Prefab Offset");
            prefabOffsetField.value = settings.prefabOffset;
            prefabOffsetField.RegisterValueChangedCallback(evt =>
            {
                settings.prefabOffset = evt.newValue;
                RepaintPreview();
            });
            transformGroup.Add(prefabOffsetField);

            // Advanced Layer Settings Group
            var advancedGroup = CreateAdvancedGroup(scrollView);

            // Render Buttons
            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("button-container");

            var renderButton = new Button(OnRenderClicked) { text = "Render & Save Icon" };
            renderButton.AddToClassList("render-button");
            buttonContainer.Add(renderButton);

            scrollView.Add(buttonContainer);

            // Preview and Status
            previewImage = new Image();
            previewImage.AddToClassList("preview-image");
            scrollView.Add(previewImage);

            statusLabel = new Label("Ready");
            statusLabel.AddToClassList("status-label");
            scrollView.Add(statusLabel);

            rootVisualElement.Add(scrollView);
        }

        private VisualElement CreateSettingsGroup(string title)
        {
            var group = new VisualElement();
            group.AddToClassList("settings-group");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("group-title");
            group.Add(titleLabel);

            return group;
        }

        private VisualElement CreateAdvancedGroup(ScrollView parentScroll)
        {
            var advancedGroup = new Foldout { text = "Advanced Layer Settings" };
            advancedGroup.AddToClassList("advanced-group");
            parentScroll.Add(advancedGroup);

            // Background Layer Settings
            var bgLabel = new Label("Background");
            bgLabel.AddToClassList("layer-title");
            advancedGroup.Add(bgLabel);

            backgroundSpriteField = new ObjectField("Background Sprite");
            backgroundSpriteField.objectType = typeof(Sprite);
            backgroundSpriteField.RegisterValueChangedCallback(evt =>
            {
                settings.backgroundSprite = evt.newValue as Sprite;
                RepaintPreview();
            });
            advancedGroup.Add(backgroundSpriteField);

            backgroundZoomField = new FloatField("Background Zoom");
            backgroundZoomField.value = settings.backgroundZoom;
            backgroundZoomField.RegisterValueChangedCallback(evt =>
            {
                settings.backgroundZoom = Mathf.Clamp(evt.newValue, 0.1f, 100f);
                backgroundZoomField.value = settings.backgroundZoom;
                RepaintPreview();
            });
            advancedGroup.Add(backgroundZoomField);

            backgroundOffsetField = new Vector2Field("Background Offset");
            backgroundOffsetField.value = settings.backgroundOffset;
            backgroundOffsetField.RegisterValueChangedCallback(evt =>
            {
                settings.backgroundOffset = evt.newValue;
                RepaintPreview();
            });
            advancedGroup.Add(backgroundOffsetField);

            tintBackgroundToggle = new Toggle("Tint Background");
            tintBackgroundToggle.value = settings.tintBackground;
            tintBackgroundToggle.RegisterValueChangedCallback(evt =>
            {
                settings.tintBackground = evt.newValue;
                backgroundTintColorField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                RepaintPreview();
            });
            advancedGroup.Add(tintBackgroundToggle);

            backgroundTintColorField = new ColorField("Background Tint Color");
            backgroundTintColorField.value = settings.backgroundTintColor;
            backgroundTintColorField.style.display = settings.tintBackground ? DisplayStyle.Flex : DisplayStyle.None;
            backgroundTintColorField.RegisterValueChangedCallback(evt =>
            {
                settings.backgroundTintColor = evt.newValue;
                RepaintPreview();
            });
            advancedGroup.Add(backgroundTintColorField);

            // Separator
            advancedGroup.Add(new VisualElement { style = { marginTop = 10, marginBottom = 10 } });

            // Frame Layer Settings
            var frameLabel = new Label("Frame");
            frameLabel.AddToClassList("layer-title");
            advancedGroup.Add(frameLabel);

            frameSpriteField = new ObjectField("Frame Sprite");
            frameSpriteField.objectType = typeof(Sprite);
            frameSpriteField.RegisterValueChangedCallback(evt =>
            {
                settings.frameSprite = evt.newValue as Sprite;
                RepaintPreview();
            });
            advancedGroup.Add(frameSpriteField);

            frameZoomField = new FloatField("Frame Zoom");
            frameZoomField.value = settings.frameZoom;
            frameZoomField.RegisterValueChangedCallback(evt =>
            {
                settings.frameZoom = Mathf.Clamp(evt.newValue, 0.1f, 20f);
                frameZoomField.value = settings.frameZoom;
                RepaintPreview();
            });
            advancedGroup.Add(frameZoomField);

            frameOffsetField = new Vector2Field("Frame Offset");
            frameOffsetField.value = settings.frameOffset;
            frameOffsetField.RegisterValueChangedCallback(evt =>
            {
                settings.frameOffset = evt.newValue;
                RepaintPreview();
            });
            advancedGroup.Add(frameOffsetField);

            tintFrameToggle = new Toggle("Tint Frame");
            tintFrameToggle.value = settings.tintFrame;
            tintFrameToggle.RegisterValueChangedCallback(evt =>
            {
                settings.tintFrame = evt.newValue;
                frameTintColorField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                RepaintPreview();
            });
            advancedGroup.Add(tintFrameToggle);

            frameTintColorField = new ColorField("Frame Tint Color");
            frameTintColorField.value = settings.frameTintColor;
            frameTintColorField.style.display = settings.tintFrame ? DisplayStyle.Flex : DisplayStyle.None;
            frameTintColorField.RegisterValueChangedCallback(evt =>
            {
                settings.frameTintColor = evt.newValue;
                RepaintPreview();
            });
            advancedGroup.Add(frameTintColorField);

            return advancedGroup;
        }

        private void RepaintPreview()
        {
            if (settings.prefab == null)
            {
                previewImage.image = null;
                statusLabel.text = "Select a prefab to preview";
                return;
            }

            var result = engine.RenderToTexture();
            if (result.Success && result.Texture != null)
            {
                previewImage.image = result.Texture;
                statusLabel.text = "Preview ready";
            }
            else
            {
                previewImage.image = null;
                statusLabel.text = $"Error: {result.ErrorMessage}";
            }
        }

        private void OnRenderClicked()
        {
            if (settings.prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a prefab.", "OK");
                return;
            }

            statusLabel.text = "Rendering...";

            var result = engine.RenderAndSave();
            if (result.Success)
            {
                statusLabel.text = $"Saved: {result.SavedPath}";
                Debug.Log($"[PrefabIconRenderer] Icon saved: {result.SavedPath}");
                EditorUtility.DisplayDialog("Success", $"Icon saved to:\n{result.SavedPath}", "OK");
            }
            else
            {
                statusLabel.text = $"Error: {result.ErrorMessage}";
                Debug.LogError($"[PrefabIconRenderer] Render error: {result.ErrorMessage}");
                EditorUtility.DisplayDialog("Error", $"Failed to render icon:\n{result.ErrorMessage}", "OK");
            }
        }

        private StyleSheet LoadStyleSheet()
        {
            // Find stylesheet relative to this script's location
            var guids = AssetDatabase.FindAssets("PrefabIconRenderer t:StyleSheet");
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Make sure it's the right file (not a copy in different location)
                if (path.EndsWith("/PrefabIconRenderer.uss") && path.Contains("unigame.prefabicons"))
                {
                    var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                    if (styleSheet != null)
                        return styleSheet;
                }
            }

            Debug.LogWarning("[PrefabIconRenderer] Could not load stylesheet. Using default styling.");
            return null;
        }

        private void LoadDefaultSettings()
        {
            // Load from EditorPrefs
            string defaultFileName = EditorPrefs.GetString(DefaultFileNameKey, "NewIcon");
            string defaultFolder = EditorPrefs.GetString(DefaultFolderPathKey, "Assets/GeneratedSprites");

            settings.fileName = defaultFileName;
            settings.folderPath = defaultFolder;
        }

        private void BrowseFolder()
        {
            string selectedPath = EditorUtility.OpenFolderPanel(
                "Select Default Save Folder",
                "Assets",
                ""
            );

            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert absolute path to relative Assets path
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }

                folderPathField.value = selectedPath;
                EditorPrefs.SetString(DefaultFolderPathKey, selectedPath);
                settings.folderPath = selectedPath;
            }
        }
    }
}
