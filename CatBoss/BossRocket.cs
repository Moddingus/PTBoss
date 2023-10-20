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
using Terraria.GameContent.Golf;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace TopHatCatBoss.CatBoss
{
    //big rocket
    public class BossRocket : ModProjectile
    {
        /// ai[0] timer
        /// ai[1] owner

        public override string Texture => "TopHatCatBoss/CatBoss/Assets/2";

        private Vector2[] orbitPositions = new Vector2[3];
        const float BEAMLEN = 1350;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
        }
        private ref float timer => ref Projectile.ai[0];

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi / 2;

            if (timer <= 80)
                Projectile.TrackClosestPlayer(15, 20, 650);

            for (int i = 0; i < 3; ++i)
            {
                float distance = 40;
                orbitPositions[i] = Projectile.Center + Vector2.One.RotatedBy(timer * 0.05f + (i * MathHelper.Pi/3)) * 20;

                Dust.NewDust(orbitPositions[i], 3, 3, DustID.DarkCelestial);
            }



            
            timer++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return base.PreDraw(ref lightColor);
        }
    }
}

