using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace PrefabIconRenderer
{
    using System;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Основной класс логики рендера иконок
    /// Может быть использован как из EditorWindow, так и вызван напрямую из кода
    /// </summary>
    [Serializable]
    public class IconRendererEngine
    {
        private const int TempLayer = 31;
        public PrefabIconSettings settings = new();

        /// <summary>
        /// Конструктор с пустыми настройками по умолчанию
        /// </summary>
        public IconRendererEngine()
        {
            settings = new PrefabIconSettings();
        }

        /// <summary>
        /// Конструктор с предустановками
        /// </summary>
        public IconRendererEngine(PrefabIconSettings initialSettings)
        {
            settings = initialSettings ?? new PrefabIconSettings();
        }

        /// <summary>
        /// Обновить настройки рендера
        /// </summary>
        public void UpdateSettings(PrefabIconSettings newSettings)
        {
            if (newSettings == null)return;
            this.settings = newSettings;
        }

        /// <summary>
        /// Получить текущие настройки
        /// </summary>
        public PrefabIconSettings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Отрендерить указанный GameObject в иконку (без сохранения)
        /// </summary>
        public PrefabIconRenderResult RenderGameObjectToTexture(GameObject gameObject)
        {
            if (gameObject == null)
                return PrefabIconRenderResult.CreateError("GameObject is not set");

            // Временно переопределяем префаб
            var originalPrefab = settings.prefab;
            settings.prefab = gameObject;

            try
            {
                var result = RenderToTexture();
                return result;
            }
            finally
            {
                // Восстанавливаем оригинальный префаб
                settings.prefab = originalPrefab;
            }
        }

        /// <summary>
        /// Отрендерить указанный GameObject и сохранить в файл
        /// </summary>
        public PrefabIconRenderResult RenderGameObjectAndSave(GameObject gameObject, string fileName = null, string folderPath = null)
        {
            if (gameObject == null)
                return PrefabIconRenderResult.CreateError("GameObject is not set");

            // Сохраняем оригинальные параметры
            var originalPrefab = settings.prefab;
            var originalFileName = settings.fileName;
            var originalFolderPath = settings.folderPath;

            settings.prefab = gameObject;
            if (!string.IsNullOrWhiteSpace(fileName))
                settings.fileName = fileName;
            if (!string.IsNullOrWhiteSpace(folderPath))
                settings.folderPath = folderPath;

            try
            {
                var result = RenderAndSave();
                return result;
            }
            finally
            {
                // Восстанавливаем оригинальные параметры
                settings.prefab = originalPrefab;
                settings.fileName = originalFileName;
                settings.folderPath = originalFolderPath;
            }
        }

        /// <summary>
        /// Отрендерить иконку в памяти (без сохранения)
        /// </summary>
        public PrefabIconRenderResult RenderToTexture()
        {
            if (settings.prefab == null)
                return PrefabIconRenderResult.CreateError("Prefab is not set");

            try
            {
                GameObject camObj = new GameObject("PreviewCam") { hideFlags = HideFlags.HideAndDontSave };
                Camera cam = camObj.AddComponent<Camera>();

                GameObject renderGroup = CreateRenderGroup(cam, out Dictionary<GameObject, int> originalLayers);

                RenderTexture previewRT = new RenderTexture(settings.resolution, settings.resolution, 24, RenderTextureFormat.ARGB32);
                previewRT.Create();

                ConfigureCamera(cam, previewRT);

                RenderTexture currentRT = RenderTexture.active;
                RenderTexture.active = previewRT;
                cam.Render();

                Texture2D previewTexture = new Texture2D(settings.resolution, settings.resolution, TextureFormat.ARGB32, false);
                previewTexture.ReadPixels(new Rect(0, 0, settings.resolution, settings.resolution), 0, 0);
                previewTexture.Apply();

                RenderTexture.active = currentRT;
                CleanupRender(camObj, renderGroup, originalLayers, previewRT);

                return PrefabIconRenderResult.CreateSuccess(previewTexture);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IconRendererEngine] Render error: {ex.Message}\n{ex.StackTrace}");
                return PrefabIconRenderResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// Отрендерить иконку и сохранить в файл
        /// </summary>
        public PrefabIconRenderResult RenderAndSave()
        {
            if (settings.prefab == null)
                return PrefabIconRenderResult.CreateError("Prefab is not set");

            try
            {
                GameObject camObj = new GameObject("IconCam") { hideFlags = HideFlags.HideAndDontSave };
                Camera cam = camObj.AddComponent<Camera>();

                GameObject renderGroup = CreateRenderGroup(cam, out Dictionary<GameObject, int> originalLayers);

                RenderTexture rt = new RenderTexture(settings.resolution, settings.resolution, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 8;
                rt.Create();

                ConfigureCamera(cam, rt);

                RenderTexture currentRT = RenderTexture.active;
                RenderTexture.active = rt;
                cam.Render();

                Texture2D tex = new Texture2D(settings.resolution, settings.resolution, TextureFormat.ARGB32, false);
                tex.ReadPixels(new Rect(0, 0, settings.resolution, settings.resolution), 0, 0);
                tex.Apply();

                // Create folder if it doesn't exist
                if (!Directory.Exists(settings.folderPath))
                    Directory.CreateDirectory(settings.folderPath);

                string safeFileName = string.IsNullOrWhiteSpace(settings.fileName) ? settings.prefab.name + "_Icon" : settings.fileName;
                string filePath = $"{settings.folderPath}/{safeFileName}.png";
                File.WriteAllBytes(filePath, tex.EncodeToPNG());

                AssetDatabase.Refresh();

                // Configure texture import settings
                TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spritePixelsPerUnit = 100;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }

                RenderTexture.active = currentRT;
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(tex);
                CleanupRender(camObj, renderGroup, originalLayers, null);

                return PrefabIconRenderResult.CreateSaved(filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IconRendererEngine] Render and save error: {ex.Message}\n{ex.StackTrace}");
                return PrefabIconRenderResult.CreateError(ex.Message);
            }
        }

        private GameObject CreateRenderGroup(Camera cam, out Dictionary<GameObject, int> originalLayers)
        {
            GameObject group = new GameObject("RenderGroup") { hideFlags = HideFlags.HideAndDontSave };
            originalLayers = new Dictionary<GameObject, int>();

            Vector3 center = cam.transform.position + cam.transform.forward * 5f;

            // Background layer
            if (settings.backgroundSprite != null)
            {
                GameObject bg = new GameObject("Background") { hideFlags = HideFlags.HideAndDontSave };
                bg.transform.SetParent(group.transform);
                var sr = bg.AddComponent<SpriteRenderer>();
                sr.sprite = settings.backgroundSprite;
                sr.sortingOrder = -100;
                sr.color = settings.tintBackground ? settings.backgroundTintColor : Color.white;
                bg.transform.position = center + cam.transform.right * settings.backgroundOffset.x + cam.transform.up * settings.backgroundOffset.y;
                bg.transform.localScale = Vector3.one * settings.backgroundZoom;
                bg.transform.rotation = Quaternion.identity;
            }

            // Main prefab object
            GameObject prefabObject = UnityEngine.Object.Instantiate(settings.prefab);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;
            prefabObject.transform.SetParent(group.transform);
            prefabObject.transform.localPosition = Vector3.zero;
            prefabObject.transform.localRotation = Quaternion.Euler(settings.objectRotation);
            prefabObject.transform.localScale = Vector3.one * settings.prefabZoom;

            Bounds bounds = GetRenderableBounds(prefabObject);
            Vector3 visualCenterOffset = prefabObject.transform.position - bounds.center;
            prefabObject.transform.position += visualCenterOffset;
            prefabObject.transform.position += cam.transform.right * settings.prefabOffset.x + cam.transform.up * settings.prefabOffset.y;

            // Sorting helper for proper sprite ordering
            GameObject sortingHelper = new GameObject("SortingHelper") { hideFlags = HideFlags.HideAndDontSave };
            sortingHelper.transform.SetParent(group.transform);
            sortingHelper.transform.position = prefabObject.transform.position;
            var helperSR = sortingHelper.AddComponent<SpriteRenderer>();
            helperSR.enabled = false;
            helperSR.sortingOrder = 0;

            // Frame layer
            if (settings.frameSprite != null)
            {
                GameObject frame = new GameObject("Frame") { hideFlags = HideFlags.HideAndDontSave };
                frame.transform.SetParent(group.transform);
                var sr = frame.AddComponent<SpriteRenderer>();
                sr.sprite = settings.frameSprite;
                sr.sortingOrder = 100;
                sr.color = settings.tintFrame ? settings.frameTintColor : Color.white;
                frame.transform.position = center + cam.transform.right * settings.frameOffset.x + cam.transform.up * settings.frameOffset.y;
                frame.transform.localScale = Vector3.one * settings.frameZoom;
                frame.transform.rotation = Quaternion.identity;
            }

            // Set all objects to temp layer
            foreach (Transform child in group.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = child.gameObject;
                originalLayers[go] = go.layer;
                go.layer = TempLayer;
            }

            return group;
        }

        private void ConfigureCamera(Camera cam, RenderTexture targetRT)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = settings.transparentBackground ? new Color(0, 0, 0, 0) : settings.backgroundColor;
            cam.targetTexture = targetRT;
            cam.orthographic = true;
            cam.orthographicSize = settings.cameraZoom;
            cam.opaqueSortMode = UnityEngine.Rendering.OpaqueSortMode.Default;
            cam.cullingMask = 1 << TempLayer;

            cam.transform.position = Vector3.forward * -10f;
            cam.transform.rotation = Quaternion.identity;
        }

        private Bounds GetRenderableBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one * 0.5f);

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private void CleanupRender(GameObject camObj, GameObject renderGroup, Dictionary<GameObject, int> originalLayers, RenderTexture rt)
        {
            if (rt != null)
                rt.Release();

            UnityEngine.Object.DestroyImmediate(camObj);
            RestoreLayers(originalLayers);
            Object.DestroyImmediate(renderGroup);
        }

        private void RestoreLayers(Dictionary<GameObject, int> layerData)
        {
            foreach (var pair in layerData)
            {
                if (pair.Key != null)
                    pair.Key.layer = pair.Value;
            }
        }
    }
}
