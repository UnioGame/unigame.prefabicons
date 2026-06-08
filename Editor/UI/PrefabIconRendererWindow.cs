using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string SourceFoldersKey = "PrefabIconRenderer_SourceFolders";
        private const string AutoNamingKey = "PrefabIconRenderer_AutoNaming";
        private const string CollisionPolicyKey = "PrefabIconRenderer_CollisionPolicy";
        private const char SourceFolderSeparator = '|';
        private const string NoPrefabsFoundLabel = "No prefabs found";

        private IconRendererEngine engine;
        private PrefabIconSettings settings;

        private readonly List<string> sourceFolders = new();
        private readonly List<PrefabEntry> foundPrefabs = new();
        private readonly List<string> prefabPopupChoices = new();

        private ObjectField prefabField;
        private IntegerField resolutionField;
        private TextField fileNameField;
        private TextField defaultFileNameField;
        private TextField folderPathField;
        private Toggle autoNamingToggle;
        private EnumField nameCollisionPolicyField;
        private Toggle transparentBackgroundToggle;
        private ColorField backgroundColorField;

        private ObjectField sourceFolderAddField;
        private VisualElement sourceFoldersContainer;
        private VisualElement prefabPopupContainer;
        private PopupField<string> prefabPopupField;
        private Label prefabSourcesStatusLabel;

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

            LoadDefaultSettings();

            BuildUI();
            RefreshPrefabs();
            RepaintPreview();
        }

        private void OnDisable()
        {
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            var styleSheet = LoadStyleSheet();
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            var mainLayout = new VisualElement();
            mainLayout.AddToClassList("main-layout");

            var inspectorScroll = new ScrollView();
            inspectorScroll.AddToClassList("inspector-scroll");

            var previewPanel = new VisualElement();
            previewPanel.AddToClassList("preview-panel");

            var titleLabel = new Label(WindowTitle);
            titleLabel.AddToClassList("title");
            inspectorScroll.Add(titleLabel);

            var sourcesGroup = CreatePrefabSourcesGroup();
            inspectorScroll.Add(sourcesGroup);

            var mainGroup = CreateFoldoutGroup("Main Settings", true);
            inspectorScroll.Add(mainGroup);

            prefabField = new ObjectField("Prefab");
            prefabField.objectType = typeof(GameObject);
            prefabField.RegisterValueChangedCallback(evt =>
            {
                settings.prefab = evt.newValue as GameObject;
                SelectPrefabInPopup(settings.prefab);
                RepaintPreview();
            });
            mainGroup.Add(prefabField);

            resolutionField = new IntegerField("Resolution");
            resolutionField.value = settings.resolution;
            resolutionField.RegisterValueChangedCallback(evt =>
            {
                settings.resolution = Mathf.Clamp(evt.newValue, 128, 1024);
                resolutionField.SetValueWithoutNotify(settings.resolution);
                RepaintPreview();
            });
            mainGroup.Add(resolutionField);

            fileNameField = new TextField("Icon Filename");
            fileNameField.value = settings.fileName;
            fileNameField.SetEnabled(!settings.autoNaming);
            fileNameField.RegisterValueChangedCallback(evt => settings.fileName = evt.newValue);
            mainGroup.Add(fileNameField);

            autoNamingToggle = new Toggle("Auto Naming");
            autoNamingToggle.value = settings.autoNaming;
            autoNamingToggle.RegisterValueChangedCallback(evt =>
            {
                settings.autoNaming = evt.newValue;
                EditorPrefs.SetBool(AutoNamingKey, settings.autoNaming);
                fileNameField.SetEnabled(!settings.autoNaming);
            });
            mainGroup.Add(autoNamingToggle);

            nameCollisionPolicyField = new EnumField("Name Collision Policy", settings.nameCollisionPolicy);
            nameCollisionPolicyField.RegisterValueChangedCallback(evt =>
            {
                settings.nameCollisionPolicy = (PrefabIconNameCollisionPolicy)Convert.ToInt32(evt.newValue);
                EditorPrefs.SetInt(CollisionPolicyKey, (int)settings.nameCollisionPolicy);
            });
            mainGroup.Add(nameCollisionPolicyField);

            var defaultGroup = CreateFoldoutGroup("Output", true);
            inspectorScroll.Add(defaultGroup);

            defaultFileNameField = new TextField("Default Icon Name");
            defaultFileNameField.value = EditorPrefs.GetString(DefaultFileNameKey, "NewIcon");
            defaultFileNameField.RegisterValueChangedCallback(evt =>
            {
                settings.fileName = evt.newValue;
                fileNameField.SetValueWithoutNotify(settings.fileName);
                EditorPrefs.SetString(DefaultFileNameKey, evt.newValue);
            });
            defaultGroup.Add(defaultFileNameField);

            folderPathField = new TextField("Default Save Folder");
            folderPathField.value = EditorPrefs.GetString(DefaultFolderPathKey, "Assets/GeneratedSprites");
            folderPathField.RegisterValueChangedCallback(evt =>
            {
                settings.folderPath = NormalizePath(evt.newValue);
                folderPathField.SetValueWithoutNotify(settings.folderPath);
                EditorPrefs.SetString(DefaultFolderPathKey, settings.folderPath);
            });
            defaultGroup.Add(folderPathField);

            var browseButton = new Button(BrowseFolder) { text = "Browse Folder..." };
            browseButton.AddToClassList("browse-button");
            defaultGroup.Add(browseButton);

            var backgroundGroup = CreateFoldoutGroup("Background", false);
            inspectorScroll.Add(backgroundGroup);

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

            var transformGroup = CreateFoldoutGroup("Transform", true);
            inspectorScroll.Add(transformGroup);

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
                cameraZoomField.SetValueWithoutNotify(settings.cameraZoom);
                RepaintPreview();
            });
            transformGroup.Add(cameraZoomField);

            prefabZoomField = new FloatField("Prefab Zoom");
            prefabZoomField.value = settings.prefabZoom;
            prefabZoomField.RegisterValueChangedCallback(evt =>
            {
                settings.prefabZoom = Mathf.Clamp(evt.newValue, 0.1f, 20f);
                prefabZoomField.SetValueWithoutNotify(settings.prefabZoom);
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

            CreateAdvancedGroup(inspectorScroll);

            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("button-container");

            var renderButton = new Button(OnRenderClicked) { text = "Render & Save Icon" };
            renderButton.AddToClassList("render-button");
            buttonContainer.Add(renderButton);

            var batchRenderButton = new Button(OnBatchRenderClicked) { text = "Render All Found Prefabs" };
            batchRenderButton.AddToClassList("render-button");
            buttonContainer.Add(batchRenderButton);

            previewImage = new Image();
            previewImage.AddToClassList("preview-image");
            previewPanel.Add(previewImage);

            statusLabel = new Label("Ready");
            statusLabel.AddToClassList("status-label");
            previewPanel.Add(statusLabel);
            previewPanel.Add(buttonContainer);

            mainLayout.Add(inspectorScroll);
            mainLayout.Add(previewPanel);
            rootVisualElement.Add(mainLayout);
        }

        private VisualElement CreatePrefabSourcesGroup()
        {
            var group = CreateSettingsGroup("Prefab Sources");

            sourceFoldersContainer = new VisualElement();
            group.Add(sourceFoldersContainer);

            var addRow = new VisualElement();
            addRow.AddToClassList("inline-row");
            addRow.style.flexDirection = FlexDirection.Row;

            sourceFolderAddField = new ObjectField("Folder");
            sourceFolderAddField.objectType = typeof(DefaultAsset);
            sourceFolderAddField.style.flexGrow = 1;
            addRow.Add(sourceFolderAddField);

            var addButton = new Button(AddSelectedSourceFolder) { text = "Add Folder" };
            addButton.AddToClassList("compact-button");
            addRow.Add(addButton);
            group.Add(addRow);

            prefabPopupContainer = new VisualElement();
            group.Add(prefabPopupContainer);

            var refreshButton = new Button(RefreshPrefabs) { text = "Refresh Prefabs" };
            refreshButton.AddToClassList("compact-button");
            group.Add(refreshButton);

            prefabSourcesStatusLabel = new Label("No source folders selected");
            prefabSourcesStatusLabel.AddToClassList("status-label");
            group.Add(prefabSourcesStatusLabel);

            RefreshSourceFoldersUI();
            RebuildPrefabPopup();

            return group;
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

        private Foldout CreateFoldoutGroup(string title, bool expanded)
        {
            var foldout = new Foldout { text = title, value = expanded };
            foldout.AddToClassList("settings-group");
            foldout.AddToClassList("compact-foldout");
            return foldout;
        }

        private VisualElement CreateAdvancedGroup(ScrollView parentScroll)
        {
            var advancedGroup = new Foldout { text = "Advanced Layer Settings" };
            advancedGroup.AddToClassList("advanced-group");
            parentScroll.Add(advancedGroup);

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
                backgroundZoomField.SetValueWithoutNotify(settings.backgroundZoom);
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

            advancedGroup.Add(new VisualElement { style = { marginTop = 10, marginBottom = 10 } });

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
                frameZoomField.SetValueWithoutNotify(settings.frameZoom);
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

        private void AddSelectedSourceFolder()
        {
            var folder = sourceFolderAddField.value;
            if (!TryGetValidFolderPath(folder, out var path))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder asset under Assets or Packages.", "OK");
                return;
            }

            if (!sourceFolders.Contains(path))
            {
                sourceFolders.Add(path);
                SaveSourceFolders();
                RefreshSourceFoldersUI();
                RefreshPrefabs();
            }

            sourceFolderAddField.value = null;
        }

        private void RefreshSourceFoldersUI()
        {
            if (sourceFoldersContainer == null)
                return;

            sourceFoldersContainer.Clear();

            for (var i = 0; i < sourceFolders.Count; i++)
            {
                var index = i;
                var row = new VisualElement();
                row.AddToClassList("inline-row");
                row.style.flexDirection = FlexDirection.Row;

                var folderField = new ObjectField("Source Folder");
                folderField.objectType = typeof(DefaultAsset);
                folderField.value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(sourceFolders[index]);
                folderField.style.flexGrow = 1;
                folderField.RegisterValueChangedCallback(evt =>
                {
                    if (!TryGetValidFolderPath(evt.newValue, out var newPath))
                    {
                        EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder asset under Assets or Packages.", "OK");
                        folderField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<DefaultAsset>(sourceFolders[index]));
                        return;
                    }

                    if (sourceFolders.Contains(newPath) && sourceFolders[index] != newPath)
                    {
                        folderField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<DefaultAsset>(sourceFolders[index]));
                        return;
                    }

                    sourceFolders[index] = newPath;
                    SaveSourceFolders();
                    RefreshPrefabs();
                });
                row.Add(folderField);

                var removeButton = new Button(() =>
                {
                    sourceFolders.RemoveAt(index);
                    SaveSourceFolders();
                    RefreshSourceFoldersUI();
                    RefreshPrefabs();
                })
                {
                    text = "Remove"
                };
                removeButton.AddToClassList("compact-button");
                row.Add(removeButton);

                sourceFoldersContainer.Add(row);
            }
        }

        private void RefreshPrefabs()
        {
            foundPrefabs.Clear();

            var validFolders = sourceFolders
                .Where(path => AssetDatabase.IsValidFolder(path))
                .ToArray();

            if (validFolders.Length > 0)
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", validFolders);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                        continue;

                    foundPrefabs.Add(new PrefabEntry(prefab, path));
                }
            }

            foundPrefabs.Sort((left, right) =>
            {
                var nameCompare = string.Compare(left.Prefab.name, right.Prefab.name, StringComparison.OrdinalIgnoreCase);
                return nameCompare != 0
                    ? nameCompare
                    : string.Compare(left.Path, right.Path, StringComparison.OrdinalIgnoreCase);
            });

            RebuildPrefabPopup();
            SelectPrefabInPopup(settings.prefab);

            if (settings.prefab == null && foundPrefabs.Count > 0)
                SetSelectedPrefab(foundPrefabs[0].Prefab);

            if (prefabSourcesStatusLabel != null)
                prefabSourcesStatusLabel.text = $"{sourceFolders.Count} folders, {foundPrefabs.Count} prefabs found";
        }

        private void RebuildPrefabPopup()
        {
            if (prefabPopupContainer == null)
                return;

            prefabPopupContainer.Clear();

            RebuildPrefabPopupChoices();

            var choices = foundPrefabs.Count == 0
                ? new List<string> { NoPrefabsFoundLabel }
                : prefabPopupChoices;

            prefabPopupField = new PopupField<string>("Prefab From Sources", choices, 0);
            prefabPopupField.SetEnabled(foundPrefabs.Count > 0);
            prefabPopupField.RegisterValueChangedCallback(evt =>
            {
                var index = prefabPopupChoices.IndexOf(evt.newValue);
                if (index < 0 || index >= foundPrefabs.Count)
                    return;

                SetSelectedPrefab(foundPrefabs[index].Prefab);
            });

            prefabPopupContainer.Add(prefabPopupField);
        }

        private void SelectPrefabInPopup(GameObject prefab)
        {
            if (prefabPopupField == null || prefab == null || foundPrefabs.Count == 0)
                return;

            var path = AssetDatabase.GetAssetPath(prefab);
            var index = foundPrefabs.FindIndex(x => x.Path == path);
            if (index < 0)
                return;

            prefabPopupField.SetValueWithoutNotify(prefabPopupChoices[index]);
        }

        private void RebuildPrefabPopupChoices()
        {
            prefabPopupChoices.Clear();

            var nameCounts = foundPrefabs
                .GroupBy(x => x.Prefab.name)
                .ToDictionary(x => x.Key, x => x.Count());

            var usedLabels = new HashSet<string>();
            for (var i = 0; i < foundPrefabs.Count; i++)
            {
                var entry = foundPrefabs[i];
                var label = entry.Prefab.name;

                if (nameCounts[entry.Prefab.name] > 1)
                    label = $"{entry.Prefab.name} ({GetParentFolderName(entry.Path)})";

                if (!usedLabels.Add(label))
                {
                    var baseLabel = label;
                    var suffix = 2;
                    while (!usedLabels.Add(label))
                    {
                        label = $"{baseLabel} {suffix}";
                        suffix++;
                    }
                }

                prefabPopupChoices.Add(label);
            }
        }

        private static string GetParentFolderName(string assetPath)
        {
            assetPath = NormalizePath(assetPath);
            var lastSlash = assetPath.LastIndexOf('/');
            if (lastSlash <= 0)
                return "Project";

            var folderPath = assetPath.Substring(0, lastSlash);
            var parentSlash = folderPath.LastIndexOf('/');
            return parentSlash < 0
                ? folderPath
                : folderPath.Substring(parentSlash + 1);
        }

        private void SetSelectedPrefab(GameObject prefab)
        {
            settings.prefab = prefab;

            if (prefabField != null)
                prefabField.SetValueWithoutNotify(settings.prefab);

            SelectPrefabInPopup(settings.prefab);
            RepaintPreview();
        }

        private void RepaintPreview()
        {
            if (previewImage == null || statusLabel == null)
                return;

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

            if (!ValidateSaveFolder())
                return;

            statusLabel.text = "Rendering...";

            var fileName = settings.autoNaming
                ? IconRendererEngine.CreateAutoFileName(settings.prefab, settings.resolution)
                : settings.fileName;

            var result = engine.RenderAndSave(fileName, settings.nameCollisionPolicy);
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

        private void OnBatchRenderClicked()
        {
            if (sourceFolders.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please add at least one source folder.", "OK");
                return;
            }

            if (foundPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No prefabs found in selected source folders.", "OK");
                return;
            }

            if (!settings.autoNaming)
            {
                EditorUtility.DisplayDialog("Error", "Batch render requires Auto Naming.", "OK");
                return;
            }

            if (!ValidateSaveFolder())
                return;

            var originalPrefab = settings.prefab;
            var successCount = 0;
            var failCount = 0;
            var assetEditingStarted = false;
            var reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                AssetDatabase.StartAssetEditing();
                assetEditingStarted = true;

                for (var i = 0; i < foundPrefabs.Count; i++)
                {
                    var entry = foundPrefabs[i];
                    settings.prefab = entry.Prefab;

                    EditorUtility.DisplayProgressBar(
                        "Rendering Prefab Icons",
                        $"Rendering {entry.Prefab.name}",
                        i / (float)foundPrefabs.Count);

                    var fileName = IconRendererEngine.CreateAutoFileName(entry.Prefab, settings.resolution);
                    var result = engine.RenderAndSave(fileName, settings.nameCollisionPolicy, reservedPaths);
                    if (result.Success)
                    {
                        successCount++;
                        Debug.Log($"[PrefabIconRenderer] Icon saved: {result.SavedPath}");
                    }
                    else
                    {
                        failCount++;
                        Debug.LogError($"[PrefabIconRenderer] Render error for {entry.Path}: {result.ErrorMessage}");
                    }
                }
            }
            finally
            {
                settings.prefab = originalPrefab;
                prefabField.SetValueWithoutNotify(settings.prefab);
                SelectPrefabInPopup(settings.prefab);
                EditorUtility.ClearProgressBar();
                if (assetEditingStarted)
                    AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            statusLabel.text = $"Batch complete. Success: {successCount}, Failed: {failCount}";
            EditorUtility.DisplayDialog(
                "Batch Render Complete",
                $"Success: {successCount}\nFailed: {failCount}",
                "OK");
        }

        private bool ValidateSaveFolder()
        {
            settings.folderPath = NormalizePath(settings.folderPath);
            if (string.IsNullOrWhiteSpace(settings.folderPath) || !IsAllowedAssetPath(settings.folderPath))
            {
                EditorUtility.DisplayDialog("Invalid Save Folder", "Save folder must be under Assets or Packages.", "OK");
                return false;
            }

            return true;
        }

        private StyleSheet LoadStyleSheet()
        {
            var guids = AssetDatabase.FindAssets("PrefabIconRenderer t:StyleSheet");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

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
            string defaultFileName = EditorPrefs.GetString(DefaultFileNameKey, "NewIcon");
            string defaultFolder = EditorPrefs.GetString(DefaultFolderPathKey, "Assets/GeneratedSprites");

            settings.fileName = defaultFileName;
            settings.folderPath = NormalizePath(defaultFolder);
            settings.autoNaming = EditorPrefs.GetBool(AutoNamingKey, true);
            settings.nameCollisionPolicy = (PrefabIconNameCollisionPolicy)EditorPrefs.GetInt(
                CollisionPolicyKey,
                (int)PrefabIconNameCollisionPolicy.AppendNumber);
            if (!Enum.IsDefined(typeof(PrefabIconNameCollisionPolicy), settings.nameCollisionPolicy))
                settings.nameCollisionPolicy = PrefabIconNameCollisionPolicy.AppendNumber;

            sourceFolders.Clear();
            var serializedFolders = EditorPrefs.GetString(SourceFoldersKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(serializedFolders))
            {
                sourceFolders.AddRange(serializedFolders
                    .Split(SourceFolderSeparator)
                    .Select(NormalizePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path) && IsAllowedAssetPath(path))
                    .Distinct());
            }
        }

        private void BrowseFolder()
        {
            string selectedPath = EditorUtility.OpenFolderPanel(
                "Select Default Save Folder",
                "Assets",
                ""
            );

            if (string.IsNullOrEmpty(selectedPath))
                return;

            selectedPath = NormalizeSelectedFolderPath(selectedPath);
            folderPathField.value = selectedPath;
            EditorPrefs.SetString(DefaultFolderPathKey, selectedPath);
            settings.folderPath = selectedPath;
        }

        private void SaveSourceFolders()
        {
            EditorPrefs.SetString(SourceFoldersKey, string.Join(SourceFolderSeparator.ToString(), sourceFolders));
        }

        private static bool TryGetValidFolderPath(UnityEngine.Object folder, out string path)
        {
            path = folder == null ? string.Empty : NormalizePath(AssetDatabase.GetAssetPath(folder));
            return !string.IsNullOrWhiteSpace(path) && IsAllowedAssetPath(path) && AssetDatabase.IsValidFolder(path);
        }

        private static bool IsAllowedAssetPath(string path)
        {
            path = NormalizePath(path);
            return path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(path, "Packages", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSelectedFolderPath(string selectedPath)
        {
            selectedPath = NormalizePath(selectedPath);
            var dataPath = NormalizePath(Application.dataPath);
            if (selectedPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets" + selectedPath.Substring(dataPath.Length);

            return selectedPath;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').TrimEnd('/');
        }

        private sealed class PrefabEntry
        {
            public PrefabEntry(GameObject prefab, string path)
            {
                Prefab = prefab;
                Path = path;
            }

            public GameObject Prefab { get; }
            public string Path { get; }
        }
    }
}
