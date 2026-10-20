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
///
/// Resources wired into a scene through <c>[Export]</c> fields are loaded by the engine
/// itself, so <see cref="Load{T}"/> never sees them. Call <see cref="KeepAliveGraph"/> on
/// those exports (see <c>GameManager.KeepExportedResourcesAlive</c>) so their wrappers
/// can't be collected while the engine cache still holds the native resources.
/// </summary>
public static class ResourceCache
{
    private static readonly Dictionary<string, Resource> cache = new();

    // Wrappers that must never be collected: they belong to resources living in the
    // engine resource cache, which crashes if it resurrects a collected wrapper.
    private static readonly List<Resource> keepAlive = new();

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

    /// <summary>True when <paramref name="resource"/>'s wrapper is held by this cache.</summary>
    public static bool IsKeptAlive(Resource resource)
    {
        if (resource == null) return false;
        foreach (Resource kept in keepAlive)
        {
            if (ReferenceEquals(kept, resource)) return true;
        }
        return false;
    }

    /// <summary>
    /// Holds a strong managed reference to a resource and every resource reachable from
    /// its exported properties (array/dictionary contents included), recursively.
    /// Use for scene-exported custom resources, which the engine cache owns.
    /// </summary>
    public static void KeepAliveGraph(Resource root)
    {
        if (root == null) return;

        Stack<Resource> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            Resource resource = pending.Pop();
            if (IsKeptAlive(resource)) continue;
            keepAlive.Add(resource);

            foreach (Godot.Collections.Dictionary property in resource.GetPropertyList())
            {
                // Only exported script variables: they are the fields that can point at
                // other resources. Reading them is side-effect free for data assets.
                int usage = property["usage"].AsInt32();
                if ((usage & (int)PropertyUsageFlags.ScriptVariable) == 0) continue;

                CollectResources(resource.Get(property["name"].AsStringName()), pending);
            }
        }
    }

    private static void CollectResources(Variant value, Stack<Resource> pending)
    {
        switch (value.VariantType)
        {
            case Variant.Type.Object:
                if (value.AsGodotObject() is Resource resource)
                    pending.Push(resource);
                break;
            case Variant.Type.Array:
                foreach (Variant element in value.AsGodotArray())
                    CollectResources(element, pending);
                break;
            case Variant.Type.Dictionary:
                foreach (Variant element in value.AsGodotDictionary().Values)
                    CollectResources(element, pending);
                break;
        }
    }
}
