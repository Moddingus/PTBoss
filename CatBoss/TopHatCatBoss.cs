using System;
using System.Collections.Generic;
using System.IO;
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
            set => Bussy = (uint)value;
        }

        private ref float timer => ref NPC.ai[0];

        private float ShaderTimer = 0;
        private Vector2 anchor;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ShaderTimer);
            writer.WriteVector2(anchor);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ShaderTimer = reader.Read();
            anchor = reader.ReadVector2();
        }
        public override void SetStaticDefaults()
        {

            Main.npcFrameCount[Type] = 26;

            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
            NPCID.Sets.TrailingMode[NPC.type] = 0;

            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCID.Sets.BossBestiaryPriority.Add(Type);

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                CustomTexturePath = "ExampleMod/Assets/Textures/Bestiary/MinionBoss_Preview",
                PortraitScale = 0.6f, // Portrait refers to the full picture when clicking on the icon in the bestiary
                PortraitPositionYOverride = 0f,
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
        }
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
            anchor = NPC.Center;
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
            //Main.NewText($"Shockwave Active: {Filters.Scene["Shockwave"].IsActive()}, Red Active: {Filters.Scene["Red"].IsActive()}");

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
            if (timer <= 1)
            {
                Vector2 pos = new Rectangle((int)(anchor.X - 500), (int)(anchor.Y), 1000, -550).random();
                NPC.velocity = NPC.DirectionTo(pos) * 9;
            }
            if (timer == Main.rand.Next(30, 90))
            {
                NPC.velocity = Vector2.Zero;
            }
            if (timer > 89)
            {
                NPC.velocity = Vector2.Zero;
            }
            if (Main.netMode != NetmodeID.Server && !Filters.Scene["Red"].IsActive())
            {
                //Filters.Scene.Activate("Shockwave", NPC.Center).GetShader().UseColor(3, 5, 15).UseTargetPosition(NPC.Center);
            }

            if (timer > 120)
            {
                Filters.Scene.Deactivate("Red");
                timer = 0;
                AtkType = ModdingusUtils.RandomFromEnum<AttackType>();
                AIState = ActionState.Attack;
            }
        }
        public void Attack()
        {

            if (timer > 0)
            {
                switch (AtkType)
                {
                    case AttackType.Sword:
                        if (timer % 60 == 0)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                Vector2 pos = NPC.Center + Vector2.One.RotatedBy(MathHelper.TwoPi / 12 * i + (timer / 60) / 3) * 30;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, NPC.Center.DirectionTo(pos) * 15, ModContent.ProjectileType<gss>(), 220, 5);
                            }
                        }
                        if (timer == 220)
                        {
                            timer = 0;
                            AIState = ActionState.Choose;
                        }
                        break;
                    case AttackType.Staff:
                        if (timer % 60 == 0)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                Vector2 pos = NPC.Center + Vector2.One.RotatedBy(MathHelper.TwoPi / 12 * i + (timer / 60) / 3) * 30;
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, NPC.Center.DirectionTo(pos) * 15, ModContent.ProjectileType<gss>(), 220, 5);
                            }
                        }
                        if (timer == 360)
                        {
                            timer = 0;
                            AIState = ActionState.Choose;
                        }
                        break;
                    default:
                        if (timer == 1)
                        {
                            createClone(NPC.Center, 1);
                            Main.NewText("done");
                            Vector2 pos = NPC.Center + Vector2.UnitX * 20 * NPC.direction;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, NPC.Center.DirectionTo(pos), ModContent.ProjectileType<BossLaser>(), 220, 5, -1, 0, NPC.whoAmI, 20);
                        }
                        if (timer == 360)
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
            Main.instance.LoadNPC(Type);
            Texture2D t = TextureAssets.Npc[Type].Value;
            Rectangle source = new Rectangle(0, NPC.frame.Y, t.Width, NPC.frame.Height);
            for (float i = 0; i < tl; i += (float)(tl / 3))
            {
                float percent = i / tl;
                Vector2 dpos = NPC.oldPos[(int)i] - screenPos + new Vector2(t.Width / 2, NPC.height);
                //spriteBatch.Draw(t, dpos, source, Color.Purple * (1 - percent), NPC.rotation, NPC.origin(), NPC.scale, SpriteEffects.None, 0);
            }
            switch (AIState) //shit code
            ///STAKEHOLDERS:
            ///government
            ///supppliers
            ///consumers
            {
                case ActionState.Choose:
                    float opacity(int i)
                    {
                        return 1;
                        if (timer < 120)
                            return 1;
                        return ((AttackType)(i - 1) == AtkType) ? 1 : 0;
                    }
                    Vector2 offset(int i)
                    {
                        switch (i) { case 1: return Vector2.Zero; case 2: return new Vector2(-30, -15); case 4: return Vector2.Zero; case 5: return Vector2.UnitX * -5; default: return Vector2.Zero; };
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
                    break;
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
        private void createClone(Vector2 pos, int attackStyle)
        {
            int a = Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.UnitX * 5, ModContent.ProjectileType<BossClone>(), 0, 0, -1, 0, (float)(AtkType)+1 + attackStyled);
            (Main.npc[a].ModNPC as BossClone).
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
            return new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat());
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
    }
}