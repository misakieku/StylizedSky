using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Misaki.StylizedSky
{
    public class SurfaceTextureManager
    {
        public int textureAtlasSize = 1024;
        public GraphicsFormat textureFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        public int textureAtlasLastValidMip = 0;

        internal const int k_MinCookieSize = 2;

        RenderTexture m_TempRenderTexture0 = null;
        RenderTexture m_TempRenderTexture1 = null;
        // Structure for cookies used by directional
        PowerOfTwoTextureAtlas m_TextureAtlas;

        // During the light loop, when reserving space for the cookies (first part of the light loop) the atlas
        // can run out of space, in this case, we set to true this flag which will trigger a re-layouting of the
        // atlas (sort entries by size and insert them again).
        bool m_2DCookieAtlasNeedsLayouting = false;
        bool m_NoMoreSpace = false;
        readonly int cookieAtlasLastValidMip;
        readonly GraphicsFormat cookieFormat;

        public SurfaceTextureManager()
        {
            int cookieAtlasSize = textureAtlasSize;
            cookieFormat = textureFormat;
            cookieAtlasLastValidMip = textureAtlasLastValidMip;

            m_TextureAtlas = new PowerOfTwoTextureAtlas(cookieAtlasSize, textureAtlasLastValidMip, textureFormat, name: "Texture Atlas", useMipMap: true);
        }

        public void NewFrame()
        {
            m_TextureAtlas.ResetRequestedTexture();
            m_2DCookieAtlasNeedsLayouting = false;
            m_NoMoreSpace = false;
        }

        public void Release()
        {
            if (m_TempRenderTexture0 != null)
            {
                m_TempRenderTexture0.Release();
                m_TempRenderTexture0 = null;
            }
            if (m_TempRenderTexture1 != null)
            {
                m_TempRenderTexture1.Release();
                m_TempRenderTexture1 = null;
            }

            if (m_TextureAtlas != null)
            {
                m_TextureAtlas.Release();
                m_TextureAtlas = null;
            }
        }

        void ReserveTempTextureIfNeeded(CommandBuffer cmd, int mipMapCount)
        {
            if (m_TempRenderTexture0 == null)
            {
                // TODO: we don't need to allocate two temp RT, we can use the atlas as temp render texture
                // it will avoid additional copy of the whole mip chain into the atlas.
                int sourceWidth = m_TextureAtlas.AtlasTexture.rt.width;
                int sourceHeight = m_TextureAtlas.AtlasTexture.rt.height;

                string cacheName = m_TextureAtlas.AtlasTexture.name;
                m_TempRenderTexture0 = new RenderTexture(sourceWidth, sourceHeight, 1, cookieFormat)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = cacheName + "TempAreaLightRT0"
                };

                // Clear the textures to avoid filtering with NaNs on consoles.
                for (int mipIdx = 0; mipIdx < mipMapCount; ++mipIdx)
                {
                    cmd.SetRenderTarget(m_TempRenderTexture0, mipIdx);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                }

                // We start by a horizontal gaussian into mip 1 that reduces the width by a factor 2 but keeps the same height
                m_TempRenderTexture1 = new RenderTexture(sourceWidth >> 1, sourceHeight, 1, cookieFormat)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    name = cacheName + "TempAreaLightRT1"
                };

                // Clear the textures to avoid filtering with NaNs on consoles.
                for (int mipIdx = 0; mipIdx < mipMapCount - 1; ++mipIdx)
                {
                    cmd.SetRenderTarget(m_TempRenderTexture1, mipIdx);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                }
            }
        }

        public void LayoutIfNeeded()
        {
            if (!m_2DCookieAtlasNeedsLayouting)
                return;

            if (!m_TextureAtlas.RelayoutEntries())
            {
                Debug.LogError($"No more space in the 2D Cookie Texture Atlas. To solve this issue, increase the resolution of the cookie atlas in the HDRP settings.");
                m_NoMoreSpace = true;
            }
        }

        public Vector4 Fetch2DCookie(CommandBuffer cmd, Texture cookie)
        {
            if (cookie.width < k_MinCookieSize || cookie.height < k_MinCookieSize)
                return Vector4.zero;

            if (!m_TextureAtlas.IsCached(out Vector4 scaleBias, m_TextureAtlas.GetTextureID(cookie)) && !m_NoMoreSpace)
                Debug.LogError($"Unity cannot fetch the 2D Light cookie texture: {cookie} because it is not on the cookie atlas. To resolve this, open your HDRP Asset and increase the resolution of the cookie atlas.");

            if (m_TextureAtlas.NeedsUpdate(cookie, false))
            {
                m_TextureAtlas.BlitTexture(cmd, scaleBias, cookie, new Vector4(1, 1, 0, 0), blitMips: false);
            }

            return scaleBias;
        }

        public void ReserveSpace(Texture cookie)
        {
            if (cookie == null)
                return;

            if (cookie.width < k_MinCookieSize || cookie.height < k_MinCookieSize)
                return;

            if (!m_TextureAtlas.ReserveSpace(cookie))
                m_2DCookieAtlasNeedsLayouting = true;
        }

        public void ResetAllocator() => m_TextureAtlas.ResetAllocator();

        public void ClearAtlasTexture(CommandBuffer cmd) => m_TextureAtlas.ClearTarget(cmd);

        public RTHandle atlasTexture => m_TextureAtlas.AtlasTexture;

        public PowerOfTwoTextureAtlas atlas => m_TextureAtlas;

        public Vector4 GetCookieAtlasSize()
        {
            return new Vector4(
                m_TextureAtlas.AtlasTexture.rt.width,
                m_TextureAtlas.AtlasTexture.rt.height,
                1.0f / m_TextureAtlas.AtlasTexture.rt.width,
                1.0f / m_TextureAtlas.AtlasTexture.rt.height
            );
        }
        public Vector4 GetCookieAtlasDatas()
        {
            float padding = Mathf.Pow(2.0f, m_TextureAtlas.mipPadding) * 2.0f;
            return new Vector4(
                m_TextureAtlas.AtlasTexture.rt.width,
                padding / (float)m_TextureAtlas.AtlasTexture.rt.width,
                cookieAtlasLastValidMip,
                0
            );
        }
    }
}