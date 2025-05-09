using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace UnnamedContentMod.Content.Tiles;

public class SakuraWood : ModTile
{
    public class SakuraWoodItem : ModItem
    {
        public override string Texture { get; } = Images.Tiles.Forest.KEY_SakuraWoodItem;

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.DefaultToPlaceableTile(ModContent.TileType<SakuraWood>());
            Item.rare = ItemRarityID.White;
            Item.value = 5;
        }
    }

    public override string Texture { get; } = Images.Tiles.Forest.KEY_SakuraWoodItem;

    public override void SetStaticDefaults()
    {
        MinPick = 2;
        HitSound = SoundID.Dig;
        Main.tileMergeDirt[Type] = true;
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
    }
}