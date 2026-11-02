using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простая полоска (фон + заливка), собираемая из кода. Подходит и для Screen Space, и для World Space канвасов.
/// </summary>
public class UIBar
{
    private RectTransform _fill;
    private Image _fillImage;

    /// <summary>
    /// Корневой RectTransform полоски.
    /// </summary>
    public RectTransform Root { get; private set; }

    /// <summary>
    /// Создаёт полоску внутри parent.
    /// </summary>
    public static UIBar Create(Transform parent, string name, Vector2 size, Color background, Color fill)
    {
        var bar = new UIBar();

        var rootGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        bar.Root = rootGo.GetComponent<RectTransform>();
        bar.Root.SetParent(parent, false);
        bar.Root.sizeDelta = size;
        rootGo.GetComponent<Image>().color = background;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        bar._fill = fillGo.GetComponent<RectTransform>();
        bar._fill.SetParent(bar.Root, false);
        bar._fill.anchorMin = Vector2.zero;
        bar._fill.anchorMax = Vector2.one;
        bar._fill.offsetMin = Vector2.zero;
        bar._fill.offsetMax = Vector2.zero;
        bar._fillImage = fillGo.GetComponent<Image>();
        bar._fillImage.color = fill;

        return bar;
    }

    /// <summary>
    /// Устанавливает заполнение от 0 до 1.
    /// </summary>
    public void SetValue(float normalized)
    {
        _fill.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
    }

    /// <summary>
    /// Меняет цвет заливки.
    /// </summary>
    public void SetFillColor(Color color)
    {
        _fillImage.color = color;
    }

    /// <summary>
    /// Создаёт текстовую подпись (встроенный шрифт, без импорта TMP).
    /// </summary>
    public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
