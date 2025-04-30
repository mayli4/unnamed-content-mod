using Microsoft.Extensions.Primitives;
using System;
using System.Runtime.CompilerServices;

namespace SourceGenerator.Generators.Assets;

/// <summary> Represents an asset file. All parameters are relative to source. </summary>
internal readonly record struct AssetFile(
    [IsExternalInit] StringSegment Path,
    [IsExternalInit] StringSegment Folder,
    [IsExternalInit] StringSegment Name,
    [IsExternalInit] StringSegment Extension,
    [IsExternalInit] AssetType AssetType
) {
    public bool Equals(AssetFile other) =>
        Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase)
        && Extension.Equals(other.Extension, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => HashCode.Combine(Path, Extension);
}