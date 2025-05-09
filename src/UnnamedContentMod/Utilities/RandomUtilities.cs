using System;
using Terraria.Utilities;

namespace UnnamedContentMod.Utilities;

public static class RandomUtilities
{
    public static int NextSign(this UnifiedRandom random)
    {
        if (random == null)
        {
            throw new ArgumentNullException(nameof(random));
        }
        return random.Next(0, 2) * 2 - 1;
    }
}