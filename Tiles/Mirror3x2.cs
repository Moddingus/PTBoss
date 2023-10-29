using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TopHatCatBoss.Tiles
{
    public class Mirror3x2 : ModTile
    {
        public override string Texture => "TopHatCatBoss/Tiles/Mirror3x2";
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Trophy"));
            DustType = 7;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            var a = new Vector2(i * 16, j * 16);
            MirrorSystem.Draw(spriteBatch, new(((int)a.X), ((int)a.Y), 48, 48), new Vector2(i * 16, j * 16));
            base.PostDraw(i, j, spriteBatch);
        }
    }
    public static class MirrorSystem
    {
        
        public static void Draw(SpriteBatch spriteBatch, Rectangle mirrorSource, Vector2 mirrorTopLeft)
        {
            for (int i = 0; i < Main.maxNPCs; ++i)
            {
                NPC npc = Main.npc[i];
                if (mirrorSource.Intersects(npc.getRect()) && npc.active)
                {
                    Main.instance.LoadNPC(npc.type);
                    Texture2D texture = TextureAssets.Npc[npc.type].Value;
                    Rectangle source = new(0, 0, texture.Height / Main.npcFrameCount[npc.type], texture.Width);
                    SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    spriteBatch.Draw(texture, mirrorSource.TopLeft()-Main.screenPosition, source, Color.White, npc.rotation, texture.center(), npc.scale, effects, 0);
                }
            }
        }
    }
}

