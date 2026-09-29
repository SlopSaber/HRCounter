using CountersPlus.Custom;
using HRCounter.Configuration;
using HRCounter.Utils;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HRCounter;

internal sealed class HRCounterCounterPreview : ICounterPreview
{
    private readonly AssetBundleManager _assetBundleManager;
    private readonly PluginConfig _config;

    public HRCounterCounterPreview(AssetBundleManager assetBundleManager, PluginConfig config)
    {
        _assetBundleManager = assetBundleManager;
        _config = config;
    }

    public void Render(CounterPreviewContext preview)
    {
        AssetBundleManager.CustomCounter? counter = _assetBundleManager.SetupCustomCounter(false);
        if (!counter.HasValue)
        {
            TMP_Text fallback = preview.CreateText();
            fallback.text = "142";
            return;
        }

        Canvas? canvas = preview.CanvasUtility.GetCanvasFromID(preview.Settings.CanvasID);
        var canvasSettings = preview.CanvasUtility.GetCanvasSettingsFromID(preview.Settings.CanvasID);
        if (canvas == null || canvasSettings == null)
        {
            Object.Destroy(counter.Value.Canvas);
            return;
        }
        float positionScale = canvasSettings.PositionScale;
        Transform container = counter.Value.Container;
        container.gameObject.name = "HRCounter Counters+ Preview";
        container.localScale = Vector3.one / 30;
        container.SetParent(canvas.transform, false);
        container.GetComponent<RectTransform>().anchoredPosition =
            preview.CanvasUtility.GetAnchoredPositionFromConfig(preview.Settings) * positionScale;

        counter.Value.Numbers.text = "142";
        counter.Value.Numbers.color = _config.Colorize ? RenderUtils.DetermineColor(142) : Color.white;
        preview.Track(container.gameObject);
        Object.Destroy(counter.Value.Canvas);
    }
}
