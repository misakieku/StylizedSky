using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    [VolumeComponentMenu("Sky/Stylized Sky")]
    [SkyUniqueID(STYLIZED_SKY_UNIQUE_ID)]
    public class StylizedSky : SkySettings
    {
        public enum ExposureType
        {
            Curve,
            Fixed,
            Automatic
        }

        const int STYLIZED_SKY_UNIQUE_ID = 39934110;

        [Tooltip("Specifies the gradient color of the sky.")]
        public GradientParameter skyGradient = new GradientParameter(new Gradient());
        [Tooltip("Specifies the gradient color of the ground.")]
        public GradientParameter groundGradient = new GradientParameter(new Gradient());
        [Tooltip("Specifies the horizon line color of the ground.")]
        public GradientParameter horizonLineGradient = new GradientParameter(new Gradient());

        [Tooltip("Specifies the state for space rendering.")]
        public BoolParameter renderSpace = new BoolParameter(false);
        [Tooltip("Specifies the texture of the space.")]
        public CubemapParameter spaceTexture = new CubemapParameter(null);
        [Tooltip("Specifies the rotation of the space.")]
        public NoInterpVector3Parameter spaceRotation = new NoInterpVector3Parameter(Vector3.zero);
        [Tooltip("Specifies the EV of the space.")]
        public FloatParameter spaceEV = new FloatParameter(0.0f);

        [Tooltip("Specifies the horizon line contribution.")]
        public ClampedFloatParameter horizonLineContribution = new ClampedFloatParameter(1, 0, 2);
        [Tooltip("Specifies the horizon line exponent.")]
        public FloatParameter horizonLineExponent = new FloatParameter(5);
        [Tooltip("Specifies the sun halo contribution.")]
        public ClampedFloatParameter sunHaloContribution = new ClampedFloatParameter(1, 0, 2);
        [Tooltip("Specifies the sun halo exponent.")]
        public FloatParameter sunHaloExponent = new FloatParameter(125);

        [Tooltip("Specifies the exposure type. Curve mode will sample the curve's value base on main light position; Fixed mode will fix the exposure value; Automatic mode will set the sky exposure base on all Stylized Directional Lights' intensity")]
        public EnumParameter<ExposureType> exposureMode = new EnumParameter<ExposureType>(ExposureType.Curve);
        [Tooltip("Specifies the sky exposure curve.")]
        public AnimationCurveParameter skyEVCurve = new AnimationCurveParameter(new AnimationCurve());
        [Tooltip("Specifies the sky exposure.")]
        public FloatParameter skyFixedExposure = new FloatParameter(1);
        [Tooltip("Specifies the sky EV adjustment.")]
        public ClampedFloatParameter skyEVAdjustment = new ClampedFloatParameter(0, -5, 5);

        public override Type GetSkyRendererType()
        {
            return typeof(StylizedSkyRenderer);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            unchecked
            {
                hash = hash * 23 + skyGradient.GetHashCode() + groundGradient.GetHashCode() +
                                    groundGradient.GetHashCode() + horizonLineGradient.GetHashCode() +
                                    horizonLineContribution.GetHashCode() + sunHaloContribution.GetHashCode() +
                                    horizonLineExponent.GetHashCode() + sunHaloExponent.GetHashCode() +
                                    skyEVCurve.GetHashCode() + renderSpace.GetHashCode() +
                                    spaceTexture.GetHashCode() + spaceRotation.GetHashCode() +
                                    spaceEV.GetHashCode() + exposureMode.GetHashCode() +
                                    skyFixedExposure.GetHashCode() + skyEVAdjustment.GetHashCode();
            }
            return hash;
        }

        public override int GetHashCode(Camera camera)
        {
            // Implement if your sky depends on the camera settings (like position for instance)
            return GetHashCode();
        }

        [Serializable]
        public sealed class GradientParameter : VolumeParameter<Gradient>
        {
            /// <summary>
            /// Creates a new <see cref="GradientParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public GradientParameter(Gradient value, bool overrideState = false) : base(value, overrideState) { }
        }
    }
}