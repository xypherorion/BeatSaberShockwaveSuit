using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.IO;

namespace ShockwaveSuit
{
    public class ModConfiguration {
        public bool modEnabled = true;
        public bool exampleCheck = false;
        public enum HapticsResponseMode {
            SaberPulse,
            OnSlash,
            OnMiss
        }
        public HapticsResponseMode hapticsMode = HapticsResponseMode.OnSlash;
        public bool ledResponse = true;
        public int saberPulseDelay = 50;

        public bool debug = false;
        public bool verbose = false;
        public static IEnumerator coLoadBundleFromStreamingAssets(string bundleName) {
            // Loading asset bundle
            var req = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, bundleName));
            yield return req;

            Assert.IsNotNull(req.assetBundle, "AssetBundleLoader : asset bundle wans't loaded from streaming assets");
            //Assert.IsNotNull(handler, "No callback handler");

            if (req.assetBundle == null) {
                OnAssetBundleLoaded(null);
                yield break;
            }

            OnAssetBundleLoaded(req.assetBundle);
        }

        public static Action OnBundleAssetsLoaded;

        private static async Task WaitForAssetLoad(AssetBundleRequest req) {
            while (!req.isDone)
                await Task.Delay(100);

            if (req.isDone)
                OnBundleAssetsLoaded();
        }

        public static async void OnAssetBundleLoaded(AssetBundle bundle) {
            if(bundle != null)
                await WaitForAssetLoad(bundle.LoadAllAssetsAsync());
        }

        public static ModConfiguration FromJson(string json) {
            return JsonConvert.DeserializeObject<ModConfiguration>(json);
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
