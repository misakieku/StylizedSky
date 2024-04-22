using UnityEngine;
using UnityEngine.Rendering;

namespace Misaki.StylizedSky
{
    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL(PackingRules.Exact, false)]
    struct StylizedDirectionalData
    {
        public Vector3 color;
        public float radius;

        public Vector3 forward;
        public float distanceFromCamera;  // -1 -> no sky interaction
        public Vector3 right;
        public float angularRadius;       // Units: radians
        public Vector3 up;
        public int type;                  // 0: star, 1: moon

        public Vector3 surfaceColor;
        public float earthshine;

        public Vector4 surfaceTextureScaleOffset; // -1 if unused (TODO: 16 bit)
        //public Texture2D surfaceTexture;

        public Vector3 sunDirection;
        public float flareCosInner;

        public Vector2 phaseAngleSinCos;
        public float flareCosOuter;
        public float flareSize;           // Units: radians

        public Vector3 flareColor;
        public float flareFalloff;
    };

}
