using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShockwaveSuit {
    class BundleLoader : MonoBehaviour {
        Coroutine co;
        public void LoadAssets(string bundleName) {
            co = StartCoroutine(ModConfiguration.coLoadBundleFromStreamingAssets(bundleName));
        }

        public void Update() {
            if (co == null)
                Destroy(gameObject);
        }
    };
}
