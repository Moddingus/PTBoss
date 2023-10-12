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
            if (!Main.dedServ)
            {
                Ref<Effect> Shockwave = new Ref<Effect>(ModContent.Request<Effect>("TopHatCatBoss/CatBoss/Assets/Shockwave", AssetRequestMode.ImmediateLoad).Value);
                
                Filters.Scene["Shockwave"] = new Filter(new ScreenShaderData(Shockwave, "Shockwave"), EffectPriority.VeryHigh);
            }
        }
    }
}