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

namespace TopHatCatBoss.CatBoss
{
    public class BossBullet : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.AmethystBolt}";

        private Vector2 init;
        public override void OnSpawn(IEntitySource source)
        {
            init = Projectile.Center;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(init);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            init = reader.ReadVector2();
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = 1;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 270;
            Projectile.light = 1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
        }
        private ref float timer => ref Projectile.ai[0];

        public override bool PreAI()
        {
            return timer > 30;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 pos = init - Main.screenPosition;
            Rectangle dest = new Rectangle(((int)pos.X), ((int)pos.Y), ((int)(1000 - timer * 100)), 2);
            Texture2D t = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(t, dest, t.source(), Color.Red, Projectile.velocity.ToRotation(), t.center(), SpriteEffects.None, 0);

            return timer < 30;
        }
    }
}