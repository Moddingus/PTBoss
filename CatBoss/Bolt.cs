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
    public class Bolt : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.AmethystBolt}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
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
            Projectile.alpha = 255;
        }
        private ref float timer => ref Projectile.ai[0];
        private int target;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(target);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            target = reader.Read();
        }
        public override void AI()
        {
            timer++;
            Projectile.alpha = (int)Math.Clamp(Projectile.alpha - timer * 3, 0, 255);

            float maxDetectRadius = 400f; // The maximum radius at which a projectile can detect a target
            float projSpeed = 12f; // The speed at which the projectile moves towards the target

            // Trying to find NPC closest to the projectile
            Player closestPlayer = FindClosestPlayer(maxDetectRadius);
            if (closestPlayer == null)
                return;

            // If found, change the velocity of the projectile and turn it in the direction of the target
            // Use the SafeNormalize extension method to avoid NaNs returned by Vector2.Normalize when the vector is zero
            Projectile.velocity = Projectile.velocity.RotatedBy(0.03f); 
        }

        // Finding the closest NPC to attack within maxDetectDistance range
        // If not found then returns null
        public Player FindClosestPlayer(float maxDetectDistance)
        {
            Player closestPlayer = null;

            // Using squared values in distance checks will let us skip square root calculations, drastically improving this method's speed.
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            // Loop through all NPCs(max always 200)
            for (int k = 0; k < Main.maxPlayers; k++)
            {
                Player target = Main.player[k];
                // Check if NPC able to be targeted. It means that NPC is
                // 1. active (alive)
                // 2. chaseable (e.g. not a cultist archer)
                // 3. max life bigger than 5 (e.g. not a critter)
                // 4. can take damage (e.g. moonlord core after all it's parts are downed)
                // 5. hostile (!friendly)
                // 6. not immortal (e.g. not a target dummy)
                if (target.active && !target.dead)
                {
                    // The DistanceSquared function returns a squared distance between 2 points, skipping relatively expensive square root calculations
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    // Check if it is within the radius
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestPlayer = target;
                    }
                }
            }

            return closestPlayer;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 b = Projectile.Center + Vector2.Normalize(Projectile.velocity).RotatedBy(10) * 160;
            Vector2 c = Projectile.Center + Vector2.Normalize(Projectile.velocity).RotatedBy(-10) * 160;
            Triangle t = new Triangle(Projectile.Center, b, c);
            ModdingusUtils.DrawTriangle(Main.spriteBatch, t);


            lightColor = Color.Black;
            Draw(Projectile);
            return false;
        }
        private static VertexStrip _vertexStrip = new VertexStrip();

        public void Draw(Projectile proj)
        {
            MiscShaderData miscShaderData = GameShaders.Misc["RainbowRod"];
            miscShaderData.UseSaturation(-2.8f);
            miscShaderData.UseOpacity(4f);
            miscShaderData.Apply();
            _vertexStrip.PrepareStripWithProceduralPadding(proj.oldPos, proj.oldRot, StripColors, StripWidth, -Main.screenPosition + proj.Size / 2f);
            _vertexStrip.DrawTrail();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        private Color StripColors(float progressOnStrip)
        {
            return Color.Black;
        }

        private float StripWidth(float progressOnStrip)
        {
            return MathHelper.Lerp(0f, 64f, MathF.Sqrt(progressOnStrip));
        }
    }
}