using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    [Serializable, VolumeComponentMenu("Sky/Stylized Cloud")]
    [CloudUniqueID(STYLIZED_CLOUD_UNIQUE_ID)]
    public class StylizedCloud : CloudSettings
    {
        const int STYLIZED_CLOUD_UNIQUE_ID = 79128564;

        public CubemapParameter clouds = new CubemapParameter(null);

        public override Type GetCloudRendererType()
        {
            return typeof(StylizedCloudRenderer);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            unchecked
            {
                hash = clouds.value != null ? hash * 23 + clouds.GetHashCode() : hash;
            }
            return hash;
        }

        public override int GetHashCode(Camera camera)
        {
            // Implement if your clouds depend on the camera settings (like position for instance)
            return GetHashCode();
        }
    }
}
