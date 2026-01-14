using UnityEngine;
using UnityEditor;

namespace PrefabIconRenderer
{
    /// <summary>
    /// Примеры использования IconRendererEngine API для рендера иконок из кода
    /// </summary>
    public static class IconRendererExamples
    {
        /// <summary>
        /// Простой пример: отрендерить и сохранить иконку с настройками по умолчанию
        /// </summary>
        [MenuItem("Tools/Prefab Icons/Example - Simple Render")]
        public static void ExampleSimpleRender()
        {
            var settings = new PrefabIconSettings
            {
                prefab = Selection.activeGameObject,
                resolution = 512,
                fileName = "MyPrefabIcon",
                folderPath = "Assets/GeneratedSprites"
            };

            var engine = new IconRendererEngine(settings);
            var result = engine.RenderAndSave();

            if (result.Success)
            {
                Debug.Log($"Icon saved successfully: {result.SavedPath}");
            }
            else
            {
                Debug.LogError($"Failed to render icon: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Пример: отрендерить иконку в памяти (без сохранения) и использовать текстуру
        /// </summary>
        [MenuItem("Tools/Prefab Icons/Example - Render to Memory")]
        public static void ExampleRenderToMemory()
        {
            var settings = new PrefabIconSettings
            {
                prefab = Selection.activeGameObject,
                resolution = 256,
                objectRotation = new Vector3(20f, 45f, 0f),
                cameraZoom = 3f
            };

            var engine = new IconRendererEngine(settings);
            var result = engine.RenderToTexture();

            if (result.Success && result.Texture != null)
            {
                // Используем текстуру, например присваиваем её материалу
                // Material mat = GetComponent<Renderer>().material;
                // mat.mainTexture = result.Texture;

                Debug.Log($"Icon rendered successfully. Texture size: {result.Texture.width}x{result.Texture.height}");
            }
            else
            {
                Debug.LogError($"Failed to render icon: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Пример: отрендерить иконку с полным набором слоев (фон, фрейм, тинты)
        /// </summary>
        [MenuItem("Tools/Prefab Icons/Example - Advanced Render")]
        public static void ExampleAdvancedRender()
        {
            // Загружаем спрайты из проекта
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/bg_frame.png");
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/icon_frame.png");

            var settings = new PrefabIconSettings
            {
                prefab = Selection.activeGameObject,
                resolution = 512,
                transparentBackground = false,
                backgroundColor = new Color(0.2f, 0.2f, 0.2f),

                objectRotation = new Vector3(25f, -30f, 0f),
                cameraZoom = 2f,
                prefabZoom = 1.2f,
                prefabOffset = new Vector2(0.1f, -0.05f),

                backgroundSprite = bgSprite,
                backgroundZoom = 1f,
                backgroundOffset = Vector2.zero,
                tintBackground = true,
                backgroundTintColor = new Color(0.8f, 0.8f, 1f), // Синеватый тинт

                frameSprite = frameSprite,
                frameZoom = 1.1f,
                frameOffset = Vector2.zero,
                tintFrame = false,

                fileName = "AdvancedIcon",
                folderPath = "Assets/GeneratedSprites"
            };

            var engine = new IconRendererEngine(settings);
            var result = engine.RenderAndSave();

            if (result.Success)
            {
                Debug.Log($"Advanced icon saved: {result.SavedPath}");
            }
            else
            {
                Debug.LogError($"Failed to render: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Пример: отрендерить несколько иконок с разными настройками
        /// </summary>
        [MenuItem("Tools/Prefab Icons/Example - Batch Render")]
        public static void ExampleBatchRender()
        {
            GameObject[] prefabsToRender = Selection.gameObjects;

            if (prefabsToRender.Length == 0)
            {
                Debug.LogWarning("Select one or more prefabs to render");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var prefab in prefabsToRender)
            {
                var settings = new PrefabIconSettings
                {
                    prefab = prefab,
                    resolution = 512,
                    fileName = prefab.name + "_Icon",
                    folderPath = "Assets/GeneratedSprites"
                };

                var engine = new IconRendererEngine(settings);
                var result = engine.RenderAndSave();

                if (result.Success)
                {
                    successCount++;
                    Debug.Log($"✓ Rendered: {prefab.name}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"✗ Failed to render {prefab.name}: {result.ErrorMessage}");
                }
            }

            Debug.Log($"Batch render completed. Success: {successCount}, Failed: {failCount}");
        }

        /// <summary>
        /// Пример: изменение настроек после создания engine
        /// </summary>
        [MenuItem("Tools/Prefab Icons/Example - Dynamic Settings")]
        public static void ExampleDynamicSettings()
        {
            var settings = new PrefabIconSettings
            {
                prefab = Selection.activeGameObject,
                resolution = 256,
                fileName = "DynamicIcon"
            };

            var engine = new IconRendererEngine(settings);

            // Меняем масштаб и отрендериваем
            settings.prefabZoom = 1.5f;
            engine.UpdateSettings(settings);

            var result1 = engine.RenderToTexture();
            Debug.Log($"Render with zoom 1.5: {(result1.Success ? "Success" : "Failed")}");

            // Меняем ротацию и отрендериваем снова
            settings.objectRotation = new Vector3(45f, 0f, 0f);
            engine.UpdateSettings(settings);

            var result2 = engine.RenderToTexture();
            Debug.Log($"Render with rotation 45: {(result2.Success ? "Success" : "Failed")}");
        }
    }
}
