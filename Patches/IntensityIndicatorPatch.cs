using MuseDashMirror;
using MelonLoader;
using MuseDashMirror.UIComponents;
using MuseDashMirror.Models;
using UnityEngine;
using UnityEngine.UI;
using MuseDashMirror.Models.PositionStrategies;

namespace TestMod.Patch
{
    public class IntensityIndicator
    {

        private GameObject intensityText;

        private GameObject canvas;
        private int stimTime;

        public IntensityIndicator()
        {
            Main.FireEvent += UpdateIntensity;

            PatchEvents.GameStartPatch += (sender, e) => stimTime++;
        }

        public void CreateIndicators(int intensity)
        {
            if (canvas == null)
            {
                canvas = CanvasUtils.CreateCameraCanvas("Indicator Canvas", CameraDimension.TwoD);
                intensityText = TextGameObjectUtils.CreateText("Intensity", canvas,
                    new EllipseTextParameters(intensity.ToString(), 40.., 40, TextAnchor.UpperLeft),
                    new TransformParameters(new Vector3(5.45f, 3.5f, 90f), new RightEdgePositionStrategy()));
            }
        }

        public void UpdateIntensity(FireData data)
        {
            if (canvas == null || intensityText == null)
            {
                canvas = CanvasUtils.CreateCameraCanvas("Indicator Canvas", CameraDimension.TwoD);
                intensityText = TextGameObjectUtils.CreateText("Intensity", canvas,
                    new EllipseTextParameters(data.strength.ToString(), 40.., 40, TextAnchor.UpperLeft),
                    new TransformParameters(new Vector3(5.45f, 3.5f, 90f), new RightEdgePositionStrategy()));
            }
            intensityText.GetComponent<Text>().text = data.strength.ToString();
        }

    }
}