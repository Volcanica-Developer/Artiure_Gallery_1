using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple in-memory cache for textures and audio clips downloaded at runtime.
///
/// This is especially useful for WebGL builds where there is no traditional file system
/// and repeated HTTP requests for the same media should be avoided.
///
/// The cache lives for the duration of the app session and is shared across all
/// callers (ArtworkManager, ArtworkManagerNew, etc.).
/// </summary>
public static class WebGLMediaCache
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();

    #region Texture Cache

    public static bool TryGetTexture(string url, out Texture2D texture)
    {
        texture = null;
        if (string.IsNullOrEmpty(url))
            return false;

        return _textureCache.TryGetValue(url, out texture) && texture != null;
    }

    public static void StoreTexture(string url, Texture2D texture)
    {
        if (string.IsNullOrEmpty(url) || texture == null)
            return;

        _textureCache[url] = texture;
    }

    #endregion

    #region Audio Cache

    public static bool TryGetAudioClip(string url, out AudioClip clip)
    {
        clip = null;
        if (string.IsNullOrEmpty(url))
            return false;

        return _audioCache.TryGetValue(url, out clip) && clip != null;
    }

    public static void StoreAudioClip(string url, AudioClip clip)
    {
        if (string.IsNullOrEmpty(url) || clip == null)
            return;

        _audioCache[url] = clip;
    }

    #endregion

    /// <summary>
    /// Clears all cached media. Call this if you want to free memory,
    /// e.g. when unloading a scene or exhibition.
    /// </summary>
    public static void ClearAll()
    {
        _textureCache.Clear();
        _audioCache.Clear();
    }
}
