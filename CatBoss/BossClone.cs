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

namespace TopHatCatBoss.CatBoss
{
    /// ai[0] timer
    /// ai[1] attack type
    /// ai[2] boss index
    /// ai[3] attack style
	public class BossClone : ModProjectile
	{
        
        public override string Texture => "TopHatCatBoss/CatBoss/TopHatCatBoss";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 3;
            Main.projFrames[Type] = 26;
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = -1;
            Projectile.width = 24;
            Projectile.height = 36;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.scale = 1.5f;
            Projectile.tileCollide = false;
            Projectile.alpha = 100;
            Projectile.damage = 0;
        }
        private ref float timer => ref Projectile.ai[0];
        private int laserIndex;
        public int attackStyle
        public override void AI()
        {
            AttackType AttackType = (AttackType)Projectile.ai[1];
            NPC owner = Main.npc[(int)Projectile.ai[2]];
            timer++;
            Projectile.alpha = (int)Math.Clamp(Projectile.alpha - timer * 3, 0, 255);
            Projectile.frame = 0;

            switch (AttackType)
            {
                case AttackType.Book:  
                    break;
                case AttackType.Sword:
                    break;
                default:
                    int i = 30;
                    
                    if (timer < i)
                    {
                        Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi / (i - 1));
                    }
                    if (timer == i + 5)
                    {
                        Projectile.velocity *= 0.5f;
                        laserIndex = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, -Vector2.UnitY, ModContent.ProjectileType<BossLaser>(), 100, 5, -1, 0, Projectile.whoAmI + Main.maxNPCs, 20);
                    }
                    if (timer >= i + 150)
                    {
                        Main.projectile[laserIndex].Kill();
                        Projectile.velocity = Projectile.DirectionTo(owner.Center) * 10;
                    }
                    if (Projectile.WithinRange(owner.Center, 20) && timer > i + 150)
                    {
                        Projectile.Kill();
                    }
                    break;
            }
            
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float tl = (float)Projectile.oldPos.Length;
            Main.instance.LoadProjectile(Type);
            Texture2D t = TextureAssets.Projectile[Type].Value;
            Rectangle source = new Rectangle(0, 0, t.Width, (int)(36));
            for (float i = 0; i < tl; i += (float)(tl / 3))
            {
                float percent = i / tl;
                Vector2 dpos = Projectile.oldPos[(int)i] - Main.screenPosition + t.center() * Projectile.scale - Vector2.UnitY * 12;
                Main.spriteBatch.Draw(t, dpos, source, Color.Purple * (1 - percent), Projectile.rotation, t.center(), Projectile.scale, SpriteEffects.None, 0);
            }
            return true;
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }
    }
}

