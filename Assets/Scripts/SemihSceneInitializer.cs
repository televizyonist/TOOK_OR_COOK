using UnityEngine;
using UnityEngine.UI;

public class SemihSceneInitializer : MonoBehaviour
{
    private Button _button;
    private Image _buttonImage;

    private void Start()
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(800f, 600f);

        canvasGo.AddComponent<GraphicRaycaster>();

        var buttonGo = new GameObject("Semih Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(canvas.transform, false);

        var rectTransform = buttonGo.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200f, 60f);
        rectTransform.anchoredPosition = Vector2.zero;

        _button = buttonGo.GetComponent<Button>();
        _buttonImage = buttonGo.GetComponent<Image>();
        _buttonImage.color = Color.white;
        _button.onClick.AddListener(ChangeButtonToRed);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(buttonGo.transform, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var label = textGo.GetComponent<Text>();
        label.text = "Semih";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(ChangeButtonToRed);
        }
    }

    private void ChangeButtonToRed()
    {
        if (_buttonImage != null)
        {
            _buttonImage.color = Color.red;
        }
    }
}
