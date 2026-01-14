using UnityEngine;
using UnityEditor;

namespace PrefabIconRenderer
{
    /// <summary>
    /// Результат рендера иконки
    /// </summary>
    public class PrefabIconRenderResult
    {
        public Texture2D Texture;
        public string SavedPath;
        public bool Success;
        public string ErrorMessage;

        public static PrefabIconRenderResult CreateError(string errorMessage)
        {
            return new PrefabIconRenderResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        public static PrefabIconRenderResult CreateSuccess(Texture2D texture)
        {
            return new PrefabIconRenderResult
            {
                Success = true,
                Texture = texture
            };
        }

        public static PrefabIconRenderResult CreateSaved(string path)
        {
            return new PrefabIconRenderResult
            {
                Success = true,
                SavedPath = path
            };
        }
    }
}
