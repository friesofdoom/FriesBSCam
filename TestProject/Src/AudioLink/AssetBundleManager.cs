using System.IO;
using UnityEngine;

namespace AudioLink.Assets
{
    internal static class AssetBundleManager
    {
        private const string PATH = "FriesBSCameraPlugin.Src.AudioLink.Bundle";

        internal static Material Material { get; private set; } = null!;

        internal static RenderTexture RenderTexture { get; private set; } = null!;

        internal static void LoadFromMemoryAsync()
        {
            if (Material != null)
                return;

            byte[] bytes;

           //var names = typeof(AssetBundleManager).Assembly.GetManifestResourceNames();
           //MyCameraPlugin.Log("Names: ");
           //foreach (var name in names)
           //{
           //    MyCameraPlugin.Log(name);
           //}

            using (Stream stream = typeof(AssetBundleManager).Assembly.GetManifestResourceStream(PATH)!)
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes, 83812045);
            Material = bundle.LoadAsset<Material>("assets/audioLink/materials/mat_audiolink.mat");
            RenderTexture = bundle.LoadAsset<RenderTexture>("assets/audiolink/rendertextures/rt_audiolink.asset");

            MyCameraPlugin.Log("Created Assets:" );
            MyCameraPlugin.Log("   " + Material.name);
            MyCameraPlugin.Log("   " + RenderTexture.name);
        }
    }
}
