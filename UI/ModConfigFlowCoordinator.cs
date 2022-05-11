using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using HMUI;

namespace ShockwaveSuit.UI {
    internal class ModConfigFlowCoordinator : FlowCoordinator {
        public void Awake() {
            ModPlugin.Log("Creating Configuration View Controller");
            if (!configViewController) configViewController = BeatSaberUI.CreateViewController<ConfigurationViewController>();
        }

        protected override void BackButtonWasPressed(ViewController topViewController) {
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this, null, ViewController.AnimationDirection.Horizontal, false);
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            try {
                if (firstActivation) {
                    SetTitle("Shockwave Suit Settings");
                    showBackButton = true;
                    ProvideInitialViewControllers(configViewController);
                }
            } catch (Exception e) {
                ModPlugin.Log(e.Message);
                if (e.InnerException != null)
                    ModPlugin.Log(e.InnerException.Message);
            }
        }

        private ConfigurationViewController configViewController = null;
    }
}
