using BeatSaberMarkupLanguage.MenuButtons;
using System;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System.Collections.Generic;
using TMPro;
using System.Linq;

namespace ShockwaveSuit.UI {
    public class ConfigurationViewController : BSMLResourceViewController {
        public override string ResourceName => "ShockwaveSuit.Views.ConfigurationView.bsml";
        
        [UIValue("enable-LEDs")]
        public bool EnableLEDs {
            get => ModPlugin.cfg.ledResponse;
            set {
                ModPlugin.cfg.ledResponse = value;
                ModPlugin.SaveModConfiguration();
            }
        }
        [UIValue("saber-pulse-delay")]
        public int SaberPulseRate {
            get => ModPlugin.cfg.saberPulseDelay;
            set {
                ModPlugin.cfg.saberPulseDelay = value;
                ModPlugin.SaveModConfiguration();
            }
        }

        [UIValue("selected-haptics-mode")]
        public object SelectedHapticsMode {
            get {
                return HapticsModeList[(int)ModPlugin.cfg.hapticsMode];
            }

            set {
                ModPlugin.cfg.hapticsMode = (ModConfiguration.HapticsResponseMode)value;
                ModPlugin.SaveModConfiguration();
            }
        }

        [UIValue("haptics-mode-list")]
        List<object> HapticsModeList = Enum.GetNames(typeof(ModConfiguration.HapticsResponseMode)).ToList<object>();
        // <text id='ban-user-id'></text>
    }
}