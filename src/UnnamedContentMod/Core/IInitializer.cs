using Terraria.ModLoader;

namespace UnnamedContentMod.Core;

/// <summary> An implementation of <see cref="ILoadable"/> that makes the <see cref="ILoadable.Unload"/> method optional. </summary>
public interface IInitializer : ILoadable
{
    void ILoadable.Load(global::Terraria.ModLoader.Mod mod) { Initialize(mod); }
    void ILoadable.Unload() { }
    void Initialize(global::Terraria.ModLoader.Mod mod);
}