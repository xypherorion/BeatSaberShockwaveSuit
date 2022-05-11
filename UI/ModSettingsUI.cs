using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using ShockwaveSuit;

namespace ShockwaveSuit.UI {
    internal class ModSettingsUI {
        public static void CreateMenu() {
            if (!Created) {
                MenuButton menuButton = new MenuButton(ModPlugin.ModName, "Shockwave Suit Settings", ShowFlow);
                MenuButtons.instance.RegisterButton(menuButton);
                Created = true;
            }
        }

        public static void ShowFlow() {
            if (spinModFlowCoordinator == null)
                spinModFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ModConfigFlowCoordinator>();
            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(spinModFlowCoordinator);
        }

        public static ModConfigFlowCoordinator spinModFlowCoordinator;
        public static bool Created;
    }
}
