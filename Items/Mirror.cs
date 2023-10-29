using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TopHatCatBoss.Tiles;

namespace TopHatCatBoss.Items
{
    public class Mirror : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<Mirror3x2>());

            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(0, 1);
        }
    }
}