
namespace Asgard.Resource
{
    public class ScriptObjResource : BaseResource
    {
        public ScriptObjResource(ABResMapItemScriptObj abResMapItem, ResourceState resourceState, Storage storage)
            : base(abResMapItem, resourceState, storage)
        {

        }

        public ABResMapScriptObj Asset
        {
            get
            {
                return this.assetBundle.LoadAsset<ABResMapScriptObj>(this.loadName);
            }
        }

    }
}
