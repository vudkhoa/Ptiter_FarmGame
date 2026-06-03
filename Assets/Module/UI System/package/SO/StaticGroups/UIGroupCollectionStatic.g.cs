//  Automatically generated
//

using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.UIManager;
using System.Collections.Generic;
using System;

namespace BrunoMikoski.UIManager
{
    public class UIGroupCollectionStatic
    {
        private static bool hasCachedValues;
        private static UIGroupCollection cachedValues;
        
        private static bool hasCachedMain;
        private static BrunoMikoski.UIManager.UIGroup cachedMain;
        
        public static BrunoMikoski.UIManager.UIGroupCollection Values
        {
            get
            {
                if (!hasCachedValues)
                    hasCachedValues = CollectionsRegistry.Instance.TryGetCollectionByGUID(new LongGuid(5201288740882502542, -6867844229614343778), out cachedValues);
                return cachedValues;
            }
        }
        
        
        public static BrunoMikoski.UIManager.UIGroup Main
        {
            get
            {
                if (!hasCachedMain)
                    hasCachedMain = Values.TryGetItemByGUID(new LongGuid(5648016018135734384, 6491183528186622891), out cachedMain);
                return cachedMain;
            }
        }
        
        
    }
}
