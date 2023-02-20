using System.Collections.Generic;
using UnityEngine;

namespace Asgard.Resource
{
    public class TextureResource : BaseResource
    {
        public TextureResource(ABResMapItemScriptObj abResMapItem, ResourceState resourceState, Storage storage)
            : base(abResMapItem, resourceState, storage)
        {

        }

        public Texture texture
        {
            get
            {
                return this.AssetObj as Texture;
            }
        }

        protected override void OnLoaded(object obj)
        {
            base.OnLoaded(obj);
            LoadSprites();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            ClearSprites();
        }

        protected override void OnUnLoaded()
        {
            base.OnUnLoaded();
            //if (!ifLoaded)
            //    ClearSprites();
        }

        #region support sprite

        private Dictionary<string, Sprite> spritesDic = null;

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
            if (spriteName == null)
                return null;
            if (spritesDic.ContainsKey(spriteName))
            {
                return spritesDic[spriteName];
            }
            return null;
        }

        public void ClearSprites()
        {
            if (spritesDic != null)
            {
                spritesDic.Clear();
                spritesDic = null;
            }
        }

        #endregion
    }
}
