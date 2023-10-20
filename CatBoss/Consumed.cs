using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics;
using Microsoft.CodeAnalysis;
using Terraria.Localization;
using Terraria.GameContent.Events;

namespace TopHatCatBoss.CatBoss
{
	public class Consumed : ModBuff
	{
        public override string Texture => "TopHatCatBoss/CatBoss/Assets/Consumed";
        
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;  // Is it a debuff?
            Main.pvpBuff[Type] = true; // Players can give other players buffs, which are listed as pvpBuff
            Main.buffNoSave[Type] = true; // Causes this buff not to persist when exiting and rejoining the world
            BuffID.Sets.LongerExpertDebuff[Type] = true; // If this buff is a debuff, setting this to true will make this buff last twice as long on players in expert mode
        }
        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            Main.NewText("drawing");
            Color color = Color.Black;
            int num1 = TextureAssets.Extra[49].Width();
            int num2 = 10;
            Rectangle rect = Main.player[Main.myPlayer].getRect();
            rect.X -= Main.screenWidth / 2;
            rect.Y -= Main.screenHeight / 2;
            rect.Inflate((num1 - rect.Width) / 2, (num1 - rect.Height) / 2 + num2 / 2);
            rect.Offset(-(int)Main.screenPosition.X, -(int)Main.screenPosition.Y + (int)Main.player[Main.myPlayer].gfxOffY - num2);
            Rectangle destinationRectangle1 = Rectangle.Union(new Rectangle(0, 0, 1, 1), new Rectangle(rect.Right - 1, rect.Top - 1, 1, 1));
            Rectangle destinationRectangle2 = Rectangle.Union(new Rectangle(Main.screenWidth - 1, 0, 1, 1), new Rectangle(rect.Right, rect.Bottom - 1, 1, 1));
            Rectangle destinationRectangle3 = Rectangle.Union(new Rectangle(Main.screenWidth - 1, Main.screenHeight - 1, 1, 1), new Rectangle(rect.Left, rect.Bottom, 1, 1));
            Rectangle destinationRectangle4 = Rectangle.Union(new Rectangle(0, Main.screenHeight - 1, 1, 1), new Rectangle(rect.Left - 1, rect.Top, 1, 1));
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, destinationRectangle1, new Rectangle?(new Rectangle(0, 0, 1, 1)), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, destinationRectangle2, new Rectangle?(new Rectangle(0, 0, 1, 1)), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, destinationRectangle3, new Rectangle?(new Rectangle(0, 0, 1, 1)), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, destinationRectangle4, new Rectangle?(new Rectangle(0, 0, 1, 1)), color);
            spriteBatch.Draw(TextureAssets.Extra[49].Value, rect, color);
            return base.PreDraw(spriteBatch, buffIndex, ref drawParams);
        }


    }
    public class ConsumedPlayer : ModPlayer
    {
    }
}

