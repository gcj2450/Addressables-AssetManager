using System;
using System.Collections.Generic;

namespace Asgard.Resource
{
    public class BinaryResource : BaseResource
    {
        //public byte[] bytes;
        public BinaryResource(ABResMapItemScriptObj abResMapItem, ResourceState resourceState, Storage storage)
            : base(abResMapItem, resourceState, storage)
        {

        }

//        public override void OnLoaded(System.Object obj)
//        {
//#if USEAB
//            UnityEngine.WWW www = (UnityEngine.WWW)obj;
//            this.bytes = www.bytes;
//#else

//#endif
//        }
    }
}
