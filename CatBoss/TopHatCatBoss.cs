using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static tModPorter.ProgressUpdate;

namespace TopHatCatBoss.CatBoss
{

    public enum AttackType
    {
        Gun,
        Sword,
        Book,
        Staff,
        Whip
    }
    [AutoloadBossHead]
    public class TopHatCatBoss : ModNPC
    {
        private enum ActionState
        {
            Spawn,
            Choose,
            Attack,
            Move,
            Death,

        }

        private uint Bussy
        {
            get => BitConverter.SingleToUInt32Bits(NPC.ai[1]);
            set => NPC.ai[1] = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }
        private ActionState AIState
        {
            get => (ActionState)Bussy;
            set => Bussy = (uint)value;
        }
        private uint Bussy2
        {
            get => BitConverter.SingleToUInt32Bits(NPC.ai[2]);
            set => NPC.ai[2] = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }
        private AttackType AtkType
        {
            get => (AttackType)Bussy2;
            set => Bussy2 = (uint)value;
        }

        private ref float timer => ref NPC.ai[0];

        private float ShaderTimer = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ShaderTimer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ShaderTimer = reader.Read();
        }
        public override void SetStaticDefaults()
        {

            Main.npcFrameCount[Type] = 26;

            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
            NPCID.Sets.TrailingMode[NPC.type] = 3;

            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCID.Sets.BossBestiaryPriority.Add(Type);

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                CustomTexturePath = "TopHatCatBoss/CatBoss/TopHatCatBoss",
                PortraitScale = 0.6f, // Portrait refers to the full picture when clicking on the icon in the bestiary
                PortraitPositionYOverride = 0f,
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
        }
        private int[] laserIndex = new int[4];
        public override void BossLoot(ref string name, ref int potionType)
        {
            name = "Top Hat Cat";
            potionType = ItemID.SuperHealingPotion;
        }
        public override void SetDefaults()
        {
            NPC.width = 24;
            NPC.height = 36;
            NPC.scale = 1.5f;

            NPC.damage = 12;

            NPC.lifeMax = 35000;
            NPC.defense = 10;

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.knockBackResist = 0f;

            NPC.value = Item.buyPrice(gold: 25);
            NPC.SpawnWithHigherTime(30);

            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.HitSound = SoundID.NPCHit8;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            NPC.dontTakeDamage = true; //spawn animation

            NPC.ScaleStats_UseStrengthMultiplier(0.6f); //dont scale like a regular npc in different gamemodes

            if (!Main.dedServ)
            {
                //Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Ropocalypse2"); Music?
            }
        }
        public override void SetBestiary(Terraria.GameContent.Bestiary.BestiaryDatabase database, Terraria.GameContent.Bestiary.BestiaryEntry bestiaryEntry)
        {
            // Sets the description of this NPC that is listed in the bestiary
            bestiaryEntry.Info.AddRange(new List<Terraria.GameContent.Bestiary.IBestiaryInfoElement> {
                new Terraria.GameContent.Bestiary.MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new Terraria.GameContent.Bestiary.FlavorTextBestiaryInfoElement("This is a cat with a top hat edit this description")
            });
        }
        public override void OnSpawn(IEntitySource source)
        {
            AIState = ActionState.Spawn;
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest();
            }
            NPC.FaceTarget();
        }
        private ActionState oldState = ActionState.Spawn;

        public override void AI()
        {
            NPC.FaceTarget();
            NPC.spriteDirection = NPC.direction;

            if (AIState != oldState) //reset
            {
                timer = 0;
            }
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest();
            }

            Player player = Main.player[NPC.target];

            if (player.dead)
            {
                // If the targeted player is dead, flee
                NPC.velocity.Y -= 0.04f;
                // This method makes it so when the boss is in "despawn range" (outside of the screen), it despawns in 10 ticks
                NPC.EncourageDespawn(10);
                return;
            }

            ChooseAction();

            if (Main.netMode != NetmodeID.Server && !Filters.Scene["Shockwave"].IsActive())
            {
                //Filters.Scene.Activate("Shockwave", NPC.Center).GetShader().UseColor(3, 5, 15).UseTargetPosition(NPC.Center);
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene["Shockwave"].IsActive()) // This all needs to happen client-side!
            {


                Filters.Scene["Shockwave"].GetShader().UseProgress(ShaderTimer / 120).UseOpacity(1 * (1 - (ShaderTimer / 120) / 3f));
            }

            if (ShaderTimer >= 360)
            {
                Filters.Scene["Shockwave"].Deactivate();
                ShaderTimer = 0;
            }

            ShaderTimer++;
            timer++;
            oldState = AIState;
        }
        public void ChooseAction()
        {
            switch (AIState)
            {
                case ActionState.Spawn:
                    Spawn();
                    break;
                case ActionState.Choose:
                    ChooseAttack();
                    break;
                case ActionState.Attack:
                    Attack();
                    break;
                default:
                    break;
            }
        }
        public void Spawn()
        {
            if (timer < 120)
            {
                NPC.dontTakeDamage = true;
                NPC.velocity.Y = -.6f;
            }
            if (timer >= 120)
            {
                NPC.velocity = Vector2.Zero;
                ModContent.GetInstance<MCameraModifiers>().Shake(NPC.Center, 20f, 1);
            }
            if (timer >= 150)
            {
                timer = 0;
                NPC.dontTakeDamage = false;
                AIState = ActionState.Choose;
            }
        }
        public void ChooseAttack()
        {
            Player target = Main.player[NPC.target];
            if (timer <= 1)
            {
                Vector2 pos = target.Center + ModdingusUtils.randomVector();
                NPC.velocity = NPC.DirectionTo(pos) * (9 + (target.Distance(NPC.Center)/80));
            }
            if (timer == Main.rand.Next(30, 90))
            {
                NPC.velocity = Vector2.Zero;
            }
            if (timer > 89)
            {
                NPC.velocity = Vector2.Zero;
            }

            if (timer > 120)
            {
                timer = 0;
                AtkType = AttackType.Sword; //ModdingusUtils.RandomFromEnum<AttackType>();
                AIState = ActionState.Attack;
            }
        }
        public void Attack()
        {
            Player target = Main.player[NPC.target];

            if (timer > 0)
            {
                switch (AtkType)
                {
                    case AttackType.Sword:
                        if (timer % 60 == 0 && timer < 220)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                Vector2 pos = NPC.Center + Vector2.One.RotatedBy(MathHelper.TwoPi / 12 * i + (timer / 60) / 3) * 30;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, NPC.Center.DirectionTo(pos) * 15, ModContent.ProjectileType<gss>(), 220, 5);
                            }
                        }
                        if (timer == 220)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, -Vector2.UnitY, ModContent.ProjectileType<Slash>(), 220, 15, -1, NPC.whoAmI);
                            
                        }
                        if (timer == 380)
                        {
                            timer = 0;
                            AIState = ActionState.Choose;
                        }
                        break;
                    case AttackType.Gun:
                        //bullets and rockets
                        Projectile.NewProjectile()
                        break;
                    default:
                        if (timer == 1)
                        {
                            Vector2 nextPos = ModdingusUtils.randomCorner() * 550;
                            for (int i = 0; i < 3; i++)
                            {
                                Vector2 clonePos(int i)
                                {
                                    return i switch
                                    {
                                        0 => new Vector2(-1, 1),
                                        1 => new Vector2(-1, -1),
                                        _ => new Vector2(1, -1),
                                    };
                                }
                                var a = createClone(target.Center + nextPos * clonePos(i), 3, target.Center);
                                a.entranceDelay = (i + 1) * 25;
                            }
                            Vector2 pos = NPC.Center + Vector2.UnitX * 20 * NPC.direction;
                            NPC.Center = target.Center + nextPos;
                            NPC.velocity = NPC.DirectionTo(target.Center) * 5;
                        }
                        if (timer > 5 && timer < 10)
                        {
                            NPC.velocity *= 0.25f;
                        }
                        if (timer == 9)
                        {
                            int a = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.velocity, ModContent.ProjectileType<BossLaser>(), 100, 5, -1, 0, NPC.whoAmI, 20);
                            Main.projectile[a].timeLeft = 80;
                        }
                        if (timer == 89)
                        {
                            NPC.Center = (target.Center + Vector2.UnitX * 300 * ModdingusUtils.PoN1() - Vector2.UnitY * 50);
                        }
                        if (timer == 133)
                        {
                            NPC.velocity = Vector2.Zero;
                        }
                        if (timer == 134)
                        {
                            NPC.velocity = Vector2.Zero;
                            NPC.velocity = -Vector2.UnitY * 2.5f;
                            var off = Main.rand.NextFloat(0, MathHelper.TwoPi);
                            for (int a = 0; a < 4; a++)
                            {
                                laserIndex[a] = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.One.RotatedBy(off + MathHelper.PiOver2 * a), ModContent.ProjectileType<BossLaser>(), 100, 5, -1, 0, NPC.whoAmI, 20);
                            }
                        }
                        if (timer > 134 && timer < 270)
                        {
                            foreach (int c in laserIndex)
                                Main.projectile[c].velocity = Main.projectile[c].velocity.RotatedBy(0.01f);
                        }
                        if (timer == 270)
                        {
                            foreach (int c in laserIndex)
                                Main.projectile[c].Kill();
                        }
                        if (timer == 80 + 230)
                        {
                            Vector2 nextPos = ModdingusUtils.randomSide() * 550 * 1.41421356f; //sqrt 2


                            for (int i = 0; i < 3; i++)
                            {
                                var a = createClone(target.Center + nextPos.RotatedBy(MathHelper.PiOver2 * (i + 1)), 3, target.Center);
                                a.entranceDelay = (i + 1) * 25;
                            }

                            Vector2 pos = NPC.Center + Vector2.UnitX * 20 * NPC.direction;
                            NPC.Center = target.Center + nextPos;
                            NPC.velocity = NPC.DirectionTo(target.Center) * 5;
                        }
                        if (timer > 315 && timer < 320)
                        {
                            NPC.velocity *= 0.25f;
                        }
                        if (timer == 319)
                        {
                            int a = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.velocity, ModContent.ProjectileType<BossLaser>(), 100, 5, -1, 0, NPC.whoAmI, 20);
                            Main.projectile[a].timeLeft = 80;
                        }
                        if (timer == 399)
                        {
                            NPC.Center = (target.Center + Vector2.UnitX * 300 * ModdingusUtils.PoN1() - Vector2.UnitY * 50);
                        }
                        if (timer == 400)
                        {
                            timer = 0;
                            AIState = ActionState.Choose;
                        }
                        break;
                }
            }
        }
        public override void FindFrame(int frameHeight)
        {

            if (AIState == ActionState.Spawn)
            {
                NPC.frame.Y = 0;
            }

            int frameSpeed = 5;
            NPC.frameCounter += 0.5f;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float tl = (float)NPC.oldPos.Length;
            float scale = NPC.scale;
            Main.instance.LoadNPC(Type);
            Texture2D t = TextureAssets.Npc[Type].Value;
            Rectangle source = new Rectangle(0, NPC.frame.Y, t.Width, NPC.frame.Height);
            for (float i = 0; i < tl; i += (float)(tl / 3))
            {
                float percent = i / tl;
                Vector2 dpos = NPC.oldPos[(int)i] - screenPos + new Vector2(t.Width * scale / 4, NPC.height * scale / 2);
                spriteBatch.Draw(t, dpos, source, Color.Purple * (1 - percent), NPC.rotation, NPC.origin(), scale, NPC.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            }
            switch (AIState) //shit code
            {
                /*case ActionState.Choose:
                    float opacity(int i)
                    {
                        return 1;
                        if (timer < 120)
                            return 1;
                        return ((AttackType)(i - 1) == AtkType) ? 1 : 0;
                    }
                    Vector2 offset(int i)
                    {
                        return i switch
                        {
                            1 => Vector2.Zero,
                            2 => new Vector2(-30, -15),
                            4 => Vector2.Zero,
                            5 => Vector2.UnitX * -5,
                            _ => Vector2.Zero,
                        };
                        ;
                    }
                    float scale(int i)
                    {
                        switch (i) { case 1: return 0.75f; case 2: return 0.6f; case 4: return 0.4666f; case 5: return 0.5f; default: return 1; };
                    }
                    float rotation(int i)
                    {
                        switch (i) { case 1: return MathHelper.Pi / 2; case 2: return 3 * MathHelper.Pi / 4; default: return 0; }
                    }
                    if (timer > 0)
                    {
                        Texture2D texture = ModContent.Request<Texture2D>($"TopHatCatBoss/CatBoss/Assets/{1}").Value; //update path when merging :)
                        Vector2 pos = NPC.Center - screenPos + new Vector2(-85, -50) + Vector2.UnitX * (0) * 50 + offset(1);
                        spriteBatch.Draw(texture, pos, texture.source(), Color.White * opacity(1), rotation(1), texture.center(), scale(1), SpriteEffects.None, 0);
                    }
                    if (timer >= 20)
                    {
                        Texture2D texture = ModContent.Request<Texture2D>($"TopHatCatBoss/CatBoss/Assets/{2}").Value; //update path when merging :)
                        Vector2 pos = NPC.Center - screenPos + new Vector2(-85, -50) + Vector2.UnitX * (2 - 1) * 50 + offset(2);
                        spriteBatch.Draw(texture, pos, texture.source(), Color.White * opacity(2), rotation(2), texture.center(), scale(2), SpriteEffects.None, 0);
                    }
                    if (timer >= 40)
                    {
                        Texture2D texture = ModContent.Request<Texture2D>($"TopHatCatBoss/CatBoss/Assets/{3}").Value; //update path when merging :)
                        Vector2 pos = NPC.Center - screenPos + new Vector2(-85, -50) + Vector2.UnitX * (3 - 1) * 50 + offset(3);
                        spriteBatch.Draw(texture, pos, texture.source(), Color.White * opacity(3), rotation(3), texture.center(), scale(3), SpriteEffects.None, 0);
                    }
                    if (timer >= 60)
                    {
                        Texture2D texture = ModContent.Request<Texture2D>($"TopHatCatBoss/CatBoss/Assets/{4}").Value; //update path when merging :)
                        Vector2 pos = NPC.Center - screenPos + new Vector2(-85, -50) + Vector2.UnitX * (4 - 1) * 50 + offset(4);
                        spriteBatch.Draw(texture, pos, texture.source(), Color.White * opacity(4), rotation(4), texture.center(), scale(4), SpriteEffects.None, 0);
                    }
                    if (timer >= 80)
                    {
                        Texture2D texture = ModContent.Request<Texture2D>($"TopHatCatBoss/CatBoss/Assets/{5}").Value; //update path when merging :)
                        Vector2 pos = NPC.Center - screenPos + new Vector2(-85, -50) + Vector2.UnitX * (5 - 1) * 50 + offset(5);
                        spriteBatch.Draw(texture, pos, texture.source(), Color.White * opacity(5), rotation(5), texture.center(), scale(5), SpriteEffects.None, 0);
                    }
                    break;*/
                case ActionState.Death:
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

                    // Retrieve reference to shader
                    /*var deathShader = GameShaders.Misc["ExampleMod:DeathAnimation"];

                    // Reset back to default value.
                    deathShader.UseOpacity(1f);
                    // We use npc.ai[3] as a counter since the real death.
                    if (timer > 30f)
                    {
                        // Our shader uses the Opacity register to drive the effect. See ExampleEffectDeath.fx to see how the Opacity parameter factors into the shader math. 
                        deathShader.UseOpacity(1f - (timer - 30f) / 150f);
                    }
                    // Call Apply to apply the shader to the SpriteBatch. Only 1 shader can be active at a time.
                    deathShader.Apply(null);*/
                    return true;
            }
            return true;
        }
        private BossClone createClone(Vector2 pos, int attackStyle, Vector2 center = new Vector2())
        {
            int a = Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<BossClone>(), 0, 0, -1, 0, (float)(AtkType));
            var b = (Main.projectile[a].ModProjectile as BossClone);
            b.attackStyle = attackStyle;
            b.centerPoint = center;

            return b;
        }
    }
    public class MCameraModifiers : ModSystem
    {
        public void Shake(Vector2 start, float strength, float seconds)
        {
            seconds = seconds * 60;
            PunchCameraModifier modifier = new PunchCameraModifier(start, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), strength, 6f, (int)seconds, 1000f, FullName);
            Main.instance.CameraModifiers.Add(modifier);
        }
    }
    public static class ModdingusUtils
    {
        public static Vector2 randomVector()
        {
            return Vector2.UnitX.RotatedBy(Main.rand.NextFloat(0, MathHelper.TwoPi));
        }
        /// <summary>
        /// returns the center of the current frame as coords on the texture
        /// </summary>
        public static Vector2 origin(this NPC npc, int offY = 0, int offX = 0)
        {
            Main.instance.LoadNPC(npc.whoAmI);
            Texture2D texture = TextureAssets.Npc[npc.whoAmI].Value;
            return new Vector2(texture.Width / 2 + offX, npc.height / 2 + offY + npc.frame.Y);
        }
        public static Rectangle source(this Texture2D tex)
        {
            return new Rectangle(0, 0, tex.Width, tex.Height);
        }
        public static Vector2 center(this Texture2D t)
        {
            return new Vector2(t.Width / 2, t.Height / 2);
        }
        public static T RandomFromEnum<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(Main.rand.Next(v.Length));
        }
        public static Vector2 random(this Rectangle rect)
        {
            return new Vector2(Main.rand.NextFloat(rect.X, rect.X + rect.Width), Main.rand.NextFloat(rect.Y, rect.Y + rect.Height));
        }
        public static Vector2 randomCorner()
        {
            return new Vector2(PoN1(), PoN1());
        }
        public static Vector2 randomSide()
        {
            if (Main.rand.NextBool())
            {
                return new Vector2(PoN1(), 0);
            } else
            {
                return new Vector2(0, PoN1());
            }
        }
        public static int PoN1()
        {
            return Main.rand.NextBool() ? -1 : 1;
        }
        public static void DrawTriangle(SpriteBatch spriteBatch, Triangle t)
        {
            Texture2D tex = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(tex, new Rectangle((int)(t.A.X-Main.screenPosition.X), (int)(t.A.Y - Main.screenPosition.Y), (int)t.AB, 1),tex.source(),Color.Red, t.BAC/2, Vector2.Zero, SpriteEffects.None, 0);
            spriteBatch.Draw(tex, new Rectangle((int)(t.A.X - Main.screenPosition.X), (int)(t.A.Y - Main.screenPosition.Y), (int)t.AC, 1), tex.source(), Color.Red, -t.BAC / 2, Vector2.Zero, SpriteEffects.None, 0);
            spriteBatch.Draw(tex, new Rectangle((int)(t.C.X - Main.screenPosition.X), (int)(t.C.Y - Main.screenPosition.Y), (int)t.BC, 1), tex.source(), Color.Red, t.ACB / 2, Vector2.Zero, SpriteEffects.None, 0);
        }
        /// <summary>
        /// Looks for a Player in a cone towards the projectile's velocity. Ignores hostility (de)buffs (eg. Putrid Scent, Beetle Armor)
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="MaxAngle"></param>
        /// <returns>The index of the closest player who meets requirements or -1 if no player is found</returns>
        public static int FindPlayerInLineOfSight(this Projectile projectile, float MaxAngle, float MaxRange)
        {
            Vector2 b = projectile.Center + Vector2.Normalize(projectile.velocity).RotatedBy(MaxAngle) * MaxRange;
            Vector2 c = projectile.Center + Vector2.Normalize(projectile.velocity).RotatedBy(-MaxAngle) * MaxRange;
            Triangle t = new Triangle(projectile.Center, b, c);

            int temp = -1;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var target = Main.player[i];

                if (t.Contains(target.Center) && target.Distance(projectile.Center) < target.Distance(projectile.Center))
                {
                    temp = i;
                }
            }
            return temp;
        }
    }
    public struct Triangle : IEquatable<Triangle>
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;

        readonly public float AB;
        readonly public float BC;
        readonly public float AC;

        readonly public float ABC;
        readonly public float BAC;
        readonly public float ACB;

        readonly public float area;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;

            AB = A.Distance(B);
            BC = B.Distance(C);
            AC = A.Distance(C);

            ABC = (float)Math.Acos(AB * AB + AC * AC - BC * BC) / 2 * AC * AB;
            BAC = (float)Math.Acos(AB * AB + BC * BC - AC * AC) / 2 * BC * AB;
            ACB = (float)Math.Acos(AC * AC + BC * BC - AB * AB) / 2 * AC * BC;

            area = (float)Math.Abs((A.X * (B.Y - C.Y) +
                                         B.X * (C.Y - A.Y) +
                                         C.X * (A.Y - B.Y)) / 2f);
        }
        public float Area(Vector2 _A, Vector2 _B, Vector2 _C)
        {
            return Math.Abs((_A.X * (_B.Y - _C.Y) +
                         _B.X * (_C.Y - _A.Y) +
                         _C.X * (_A.Y - _B.Y)) / 2f);
        }
        public override bool Equals(object obj)
        {
            if (obj is Triangle)
            {
                return Equals((Triangle)obj);
            }
            return false;
        }
        public bool Contains(Vector2 p)
        {
            double a1 = Area(p, B, C);

            double a2 = Area(A, p, C);

            double a3 = Area(A, B, p);

            return (area == a1 + a2 + a3);
        }
        public bool Similar(Triangle other)
        {
            return other.ABC == ABC && other.BAC == BAC && other.ACB == ACB;
        }
        public override string ToString()
        {
            return $"A: {A}, B: {B}, C: {C}";
        }

        public bool Equals(Triangle other)
        {
            return other.A == A && other.B == B && other.C == C && other.AB == AB && other.BC == BC && other.AC == AC;
        }

        public static bool operator ==(Triangle left, Triangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Triangle left, Triangle right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}