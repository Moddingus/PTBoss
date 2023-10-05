using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace TopHatCatBoss
{
	public class TopHatCatBoss : Mod
	{
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Ref<Effect> Shockwave = new Ref<Effect>(ModContent.Request<Effect>("TopHatCatBoss/CatBoss/Assets/Shockwave", AssetRequestMode.ImmediateLoad).Value);
                Ref<Effect> RedScreen = new Ref<Effect>(ModContent.Request<Effect>("TopHatCatBoss/CatBoss/Assets/Shader2", AssetRequestMode.ImmediateLoad).Value);


                Filters.Scene["Shockwave"] = new Filter(new ScreenShaderData(Shockwave, "Shockwave"), EffectPriority.VeryHigh);
                Filters.Scene["Shockwave"].Load();

                Filters.Scene["Red"] = new Filter(new ScreenShaderData(RedScreen, "FilterMyShader"), EffectPriority.VeryHigh);
                Filters.Scene["Red"].Load();
            }
        }
    }
}