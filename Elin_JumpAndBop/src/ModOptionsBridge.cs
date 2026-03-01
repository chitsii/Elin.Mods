using EvilMask.Elin.ModOptions;

namespace Elin_JumpAndBop
{
    public class ModOptionsBridge
    {
        public void SetTranslations(ModOptionController controller)
        {
            controller.SetTranslation(Plugin.ModGuid,
                "Dynamic Jump & Bop",
                "Dynamic Jump & Bop",
                "Dynamic Jump & Bop");

            controller.SetTranslation("ModTooltip",
                "Adds rhythmic bounce to your PC sprite.",
                "PCスプライトにリズミカルなバウンスを追加します。",
                "为PC精灵添加节奏感弹跳动画。");

            controller.SetTranslation("EnableMod",
                "Enable Mod", "Modを有効化", "启用Mod");
            controller.SetTranslation("EnableMod_tooltip",
                "Toggle the entire mod functionality.",
                "Mod全体の機能を切り替えます。",
                "切换Mod整体功能。");

            controller.SetTranslation("JumpHeight",
                "Jump Height", "ジャンプの高さ", "跳跃高度");
            controller.SetTranslation("JumpHeight_tooltip",
                "Height of the bounce animation.",
                "バウンスアニメーションの高さ。",
                "弹跳动画的高度。");

            controller.SetTranslation("EnableOnWait",
                "Bounce on Wait", "足踏み時のバウンス", "等待时弹跳");
            controller.SetTranslation("EnableOnWait_tooltip",
                "Enable bounce when pressing Space to wait.",
                "Spaceキーで足踏み時にバウンスを有効にします。",
                "按空格等待时启用弹跳。");

            controller.SetTranslation("EnableOnSex",
                "Bounce on Intimacy", "親密行為時のバウンス", "亲密时弹跳");
            controller.SetTranslation("EnableOnSex_tooltip",
                "Enable bounce during intimate actions.",
                "親密行為中にバウンスを有効にします。",
                "亲密行为时启用弹跳。");
        }
    }
}
