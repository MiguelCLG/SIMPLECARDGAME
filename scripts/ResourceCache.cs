using System.Collections.Generic;
using Godot;

/// <summary>
/// Loads resources while keeping a strong managed reference to them and bypassing the
/// engine resource cache.
///
/// Godot's C# binding has a race where the resource cache tries to "resurrect" a resource
/// whose managed wrapper was garbage-collected but not finalized yet, producing
/// "FATAL: Condition \"gchandle.is_released()\" is true" (godotengine/godot#83762).
/// Keeping the managed side alive and using <see cref="ResourceLoader.CacheMode.Ignore"/>
/// avoids that path. Use this instead of <c>GD.Load</c>/<c>ResourceLoader.Load</c> for the
/// project's custom C# <see cref="Resource"/> assets (.tres).
/// </summary>
public static class ResourceCache
{
    private static readonly Dictionary<string, Resource> cache = new();

    public static T Load<T>(string path) where T : Resource
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (cache.TryGetValue(path, out Resource cached) && GodotObject.IsInstanceValid(cached))
            return cached as T;

        if (!ResourceLoader.Exists(path))
        {
            GD.PushError($"ResourceCache: resource not found '{path}'");
            cache[path] = null;
            return null;
        }

        // CacheMode.Ignore keeps the resource (and its dependencies) out of the engine
        // cache; the static dictionary keeps the managed wrapper alive. Together they
        // prevent the resurrection race.
        T resource = ResourceLoader.Load<T>(path, null, ResourceLoader.CacheMode.Ignore);
        if (resource == null)
            GD.PushError($"ResourceCache: failed to load '{path}'");

        cache[path] = resource;
        return resource;
    }
}
