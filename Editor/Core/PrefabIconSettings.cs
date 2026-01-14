using UnityEngine;

namespace PrefabIconRenderer
{
    using System;

    /// <summary>
    /// Настройки для рендера иконки префаба
    /// </summary>
    [Serializable]
    public class PrefabIconSettings
    {
        public GameObject prefab;
        public int resolution = 512;
        public bool transparentBackground = true;
        public Color backgroundColor = Color.white;

        public Vector3 objectRotation = new Vector3(0, 0, 0f);
        public float cameraZoom = 2f;

        public float prefabZoom = 1f;
        public Vector2 prefabOffset = Vector2.zero;

        public Sprite backgroundSprite;
        public float backgroundZoom = 1f;
        public Vector2 backgroundOffset = Vector2.zero;
        public bool tintBackground = false;
        public Color backgroundTintColor = Color.white;

        public Sprite frameSprite;
        public float frameZoom = 1f;
        public Vector2 frameOffset = Vector2.zero;
        public bool tintFrame = false;
        public Color frameTintColor = Color.white;

        public string fileName = "NewIcon";
        public string folderPath = "Assets/GeneratedSprites";

        /// <summary>
        /// Создает копию настроек
        /// </summary>
        public PrefabIconSettings Clone()
        {
            return new PrefabIconSettings
            {
                prefab = this.prefab,
                resolution = this.resolution,
                transparentBackground = this.transparentBackground,
                backgroundColor = this.backgroundColor,
                objectRotation = this.objectRotation,
                cameraZoom = this.cameraZoom,
                prefabZoom = this.prefabZoom,
                prefabOffset = this.prefabOffset,
                backgroundSprite = this.backgroundSprite,
                backgroundZoom = this.backgroundZoom,
                backgroundOffset = this.backgroundOffset,
                tintBackground = this.tintBackground,
                backgroundTintColor = this.backgroundTintColor,
                frameSprite = this.frameSprite,
                frameZoom = this.frameZoom,
                frameOffset = this.frameOffset,
                tintFrame = this.tintFrame,
                frameTintColor = this.frameTintColor,
                fileName = this.fileName,
                folderPath = this.folderPath
            };
        }
    }
}
