using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaulMapper
{
    public static class PaulActions
    {
        public static readonly Type[] actionMaps = new Type[]
        {
            typeof(CMInput.ICameraActions),
            typeof(CMInput.IBeatmapObjectsActions),
            typeof(CMInput.INodeEditorActions),
            typeof(CMInput.ISavingActions),
            typeof(CMInput.ITimelineActions),
            typeof(CMInput.IPlaybackActions)
        };

        public static Type[] actionMapsDisabled
        {
            get
            {
                return (from x in typeof(CMInput).GetNestedTypes()
                        where x.IsInterface && !actionMaps.Contains(x)
                        select x).ToArray<Type>();
            }
        }
    }
}
