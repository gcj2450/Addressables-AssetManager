using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asgard.Resource
{
    public class AtlasResource : BaseResource
    {

        private Dictionary<string, Sprite> spritesDic = null;
        public AtlasResourceEnum.AtlasType atlasType = AtlasResourceEnum.AtlasType.None;

        public AtlasResource(ABResMapItemScriptObj abResMapItem, ResourceState resourceState, Storage storage)
            : base(abResMapItem, resourceState, storage)
        {
            string assetBundleName = abResMapItem.AssetBundleName;
            foreach (AtlasResourceEnum.AtlasType type in Enum.GetValues(typeof(AtlasResourceEnum.AtlasType)))
            {
#if UNITY_EDITOR && !USEAB
                string subStr = "UI/GUiResource/Atlas/";
#else
                string subStr = "ui/guiresource/atlas/";
#endif
                int indexSubStr = assetBundleName.IndexOf(subStr);
                string selfAtlasName = assetBundleName.Substring(indexSubStr);
                selfAtlasName = selfAtlasName.Substring(0, selfAtlasName.LastIndexOf("."));
                selfAtlasName = selfAtlasName.Substring(subStr.Length);
                string atlasName = Enum.GetName(typeof(AtlasResourceEnum.AtlasType), type).ToLower();
                if (selfAtlasName.ToLower().Equals(atlasName))
                {
                    this.atlasType = type;
                    return;
                }
            }
            this.atlasType = AtlasResourceEnum.AtlasType.None;
        }

        private void LoadSprites()
        {
            if (spritesDic == null) 
                spritesDic = new Dictionary<string, Sprite>();
#if UNITY_EDITOR && !USEAB
            UnityEngine.Object[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(this.abResMapItem.AssetBundleName);
#else
            UnityEngine.Object[] sprites = this.assetBundle.LoadAllAssets();
#endif
            spritesDic.Clear();
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] is Sprite)
                {
                    Sprite obj = (Sprite)sprites[i];
                    spritesDic.Add(obj.name, obj);
                }
            }
        }

        public Sprite GetSpriteByName(string spriteName)
        {
            if (spriteName == null) return null;
            if (spritesDic.ContainsKey(spriteName))
            {
                return spritesDic[spriteName];
            }
            return null;
        }

        protected override void OnLoaded(System.Object obj)
        {
            LoadSprites();
        }

        protected override void OnUnLoaded()
        {
            base.OnUnLoaded();
            //if (spritesDic != null)
            //{
            //    spritesDic.Clear();
            //    spritesDic = null;
            //}
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            if (spritesDic != null)
            {
                spritesDic.Clear();
                spritesDic = null;
            }
        }

    }
}
