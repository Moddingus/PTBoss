using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace TopHatCatBoss.Tiles
{
    public class _3x2Mirror : ModTile
    {
        public override string Texture => "TopHatCatBoss/Tiles/3x2Mirror";
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
            var a = Main.LocalPlayer.position;
            MirrorDraw.Draw(spriteBatch, new(((int)a.X), ((int)a.Y), 100, 20), new Point(i, j).ToVector2());
            base.PostDraw(i, j, spriteBatch);
        }
    }
    public static class MirrorDraw
    {
        public static void Draw(SpriteBatch spriteBatch, Rectangle mirrorSource, Vector2 mirrorTopLeft)
        {
            foreach (NPC npc in Main.npc)
            {
                if (mirrorSource.Intersects(npc.getRect()))
                {
                    Texture2D texture = TextureAssets.Npc[npc.whoAmI].Value;
                    Vector2 offset = mirrorSource.TopLeft() - mirrorTopLeft;
                    Vector2 position = mirrorSource.TopLeft() - npc.position + offset - Main.screenPosition;
                    Rectangle source = new(0, npc.frame.Y, npc.width, npc.height);
                    SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    spriteBatch.Draw(texture, position, source, Color.White, npc.rotation, texture.center(), npc.scale, effects, 0);
                }
            }
            foreach (Projectile proj in Main.projectile)
            {
                if (mirrorSource.Intersects(proj.getRect()))
                {
                    //projectiles.Add(proj);
                }
            }


            
        }
    }
}

