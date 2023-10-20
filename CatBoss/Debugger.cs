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
	public class Debugger : ModItem
	{
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.autoReuse = true;

            Item.DamageType = DamageClass.Melee;
            Item.damage = 999999999;
            Item.knockBack = 99999;
            Item.crit = 50;

            Item.value = Item.buyPrice(copper: 1);
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
        }
        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Consumed>(), 120);
            return base.UseItem(player);
        }
    }
}

