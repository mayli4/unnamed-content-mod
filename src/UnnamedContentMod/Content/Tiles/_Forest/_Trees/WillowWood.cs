using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace UnnamedContentMod.Content.Tiles;

public class WillowWood : ModTile {
    public class WillowWoodItem : ModItem {
        public override string Texture { get; } = Images.Tiles.Forest.KEY_WillowWoodItem;

        public override void SetDefaults() {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.DefaultToPlaceableTile(ModContent.TileType<WillowWood>());
            Item.rare = ItemRarityID.White;
            Item.value = 5; 
        }
    }
    
    public override string Texture { get; } = Images.Tiles.Forest.KEY_WillowWoodItem;
    
    public override void SetStaticDefaults() {
        MinPick = 2;
        HitSound = SoundID.Dig;
        Main.tileMergeDirt[Type] = true;
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
    }
}