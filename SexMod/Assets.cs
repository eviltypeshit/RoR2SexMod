﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SexMod
{
    public static class Assets
    {
        private static readonly List<AssetBundle> AssetBundles = new List<AssetBundle>();
        private static readonly Dictionary<string, int> AssetIndices = new Dictionary<string, int>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetBundleLocation"></param>
        public static void AddBundle(string assetBundleLocation)
        {
            using var assetBundleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"SexMod.{assetBundleLocation}");
            AssetBundle assetBundle = AssetBundle.LoadFromStream(assetBundleStream);

            int index = AssetBundles.Count;
            AssetBundles.Add(assetBundle);

            foreach (var assetName in assetBundle.GetAllAssetNames())
            {
                string path = assetName.ToLower();

                if (path.StartsWith("assets/"))
                    path = path.Remove(0, "assets/".Length);
                AssetIndices[path] = index;
            }

            //DebugClass.Log($"Loaded AssetBundle: {assetBundleLocation}");
        }

        public static T Load<T>(string assetName) where T : UnityEngine.Object
        {
            if (assetName.Contains(":"))
            {
                string[] path = assetName.Split(':');

                assetName = path[1].ToLower();
            }
            if (assetName.StartsWith("assets/"))
                assetName = assetName.Remove(0, "assets/".Length);
            int index = AssetIndices[assetName];
            return AssetBundles[index].LoadAsset<T>($"assets/{assetName}");
        }
    }
}
