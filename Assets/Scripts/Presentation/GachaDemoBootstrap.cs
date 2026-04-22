using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;

namespace GachaDemo.Presentation
{
    public sealed class GachaDemoBootstrap : MonoBehaviour
    {
        private static TMP_FontAsset _runtimeCjkFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildIfMissing()
        {
            if (Object.FindFirstObjectByType<GachaController>() != null)
            {
                return;
            }

            EnsureMainCamera();
            EnsureEventSystem();

            var canvasGo = new GameObject("GachaCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = CreatePanel(canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(800f, 500f));
            panel.name = "Panel";
            panel.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.14f, 0.9f);
            panel.sizeDelta = new Vector2(1220f, 720f);
            TryApplySprite(panel.GetComponent<Image>(), "GachaTheme/panel_bg");

            var infoStrip = CreatePanel(panel.transform, new Vector2(0.5f, 1f), new Vector2(980f, 130f));
            infoStrip.name = "InfoStrip";
            var infoStripImage = infoStrip.GetComponent<Image>();
            infoStripImage.color = new Color(0.03f, 0.05f, 0.1f, 0.7f);
            infoStrip.anchoredPosition = new Vector2(0f, -90f);

            var titleText = CreateText(panel.transform, "TitleText", new Vector2(0f, 255f), 34);
            titleText.text = "星穹祈愿";
            titleText.color = new Color(0.95f, 0.92f, 0.75f, 1f);
            titleText.fontStyle = FontStyles.Bold;

            var currencyText = CreateText(panel.transform, "CurrencyText", new Vector2(0f, 220f), 26);
            currencyText.color = new Color(0.8f, 0.9f, 1f, 1f);
            var stateText = CreateText(panel.transform, "StateText", new Vector2(0f, 185f), 20);
            stateText.color = new Color(0.65f, 0.8f, 0.95f, 1f);
            var resultText = CreateText(panel.transform, "ResultText", new Vector2(0f, -40f), 20);
            resultText.alignment = TextAlignmentOptions.TopLeft;
            resultText.color = new Color(0.96f, 0.96f, 1f, 1f);

            var cardGrid = CreateCardGrid(panel.transform);
            var cardTemplate = CreateCardTemplate(cardGrid.transform);
            cardTemplate.SetActive(false);

            var singleBtn = CreateButton(panel.transform, "SingleDraw", "单抽", new Vector2(-220f, -300f));
            var tenBtn = CreateButton(panel.transform, "TenDraw", "十连", new Vector2(0f, -300f));
            var skipBtn = CreateButton(panel.transform, "Skip", "跳过", new Vector2(220f, -300f));
            StyleButton(singleBtn.GetComponent<Image>(), new Color(0.55f, 0.67f, 0.95f, 1f));
            StyleButton(tenBtn.GetComponent<Image>(), new Color(0.78f, 0.63f, 0.98f, 1f));
            StyleButton(skipBtn.GetComponent<Image>(), new Color(0.7f, 0.74f, 0.82f, 1f));
            TryApplySprite(singleBtn.GetComponent<Image>(), "GachaTheme/btn_single");
            TryApplySprite(tenBtn.GetComponent<Image>(), "GachaTheme/btn_ten");
            TryApplySprite(skipBtn.GetComponent<Image>(), "GachaTheme/btn_skip");

            var flash = CreatePanel(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(520f, 220f));
            var flashImg = flash.GetComponent<Image>();
            flashImg.color = new Color(1f, 1f, 1f, 0.28f);
            flashImg.raycastTarget = false;
            flash.gameObject.SetActive(false);

            var meteor = CreatePanel(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(340f, 16f));
            meteor.name = "MeteorTrail";
            var meteorImg = meteor.GetComponent<Image>();
            meteorImg.color = new Color(0.75f, 0.88f, 1f, 0.95f);
            meteor.gameObject.SetActive(false);
            meteor.transform.SetAsLastSibling();

            var fiveStarBorder = CreatePanel(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(760f, 430f));
            fiveStarBorder.name = "FiveStarBorder";
            var borderImg = fiveStarBorder.GetComponent<Image>();
            borderImg.color = new Color(1f, 1f, 1f, 0f);
            borderImg.raycastTarget = false;
            var borderOutline = fiveStarBorder.gameObject.AddComponent<Outline>();
            borderOutline.effectDistance = new Vector2(3f, 3f);
            borderOutline.effectColor = new Color(1f, 0.82f, 0.3f, 0.9f);
            fiveStarBorder.gameObject.SetActive(false);

            var rarityText = CreateText(panel.transform, "RarityText", new Vector2(0f, 120f), 42);
            rarityText.gameObject.SetActive(false);

            var reveal = panel.gameObject.AddComponent<RevealTimelineController>();
            var revealSerialized = new SerializedObjectAdapter(reveal);
            revealSerialized.Set("flashImage", flashImg);
            revealSerialized.Set("fiveStarBorder", borderImg);
            revealSerialized.Set("meteorTrail", meteorImg);
            revealSerialized.Set("rarityText", rarityText);

            var resultPanel = panel.gameObject.AddComponent<ResultPanelUI>();
            var resultSerialized = new SerializedObjectAdapter(resultPanel);
            resultSerialized.Set("resultText", resultText);
            resultSerialized.Set("cardGridRoot", cardGrid);
            resultSerialized.Set("cardTemplate", cardTemplate);

            var gachaPanel = panel.gameObject.AddComponent<GachaPanelUI>();
            var panelSerialized = new SerializedObjectAdapter(gachaPanel);
            panelSerialized.Set("singleDrawButton", singleBtn.GetComponent<Button>());
            panelSerialized.Set("tenDrawButton", tenBtn.GetComponent<Button>());
            panelSerialized.Set("skipButton", skipBtn.GetComponent<Button>());
            panelSerialized.Set("currencyText", currencyText);
            panelSerialized.Set("stateText", stateText);

            var controller = panel.gameObject.AddComponent<GachaController>();
            var controllerSerialized = new SerializedObjectAdapter(controller);
            controllerSerialized.Set("gachaPanel", gachaPanel);
            controllerSerialized.Set("resultPanel", resultPanel);
            controllerSerialized.Set("revealTimeline", reveal);
        }

        private static void EnsureMainCamera()
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem", typeof(EventSystem));
            var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                es.AddComponent(inputSystemModuleType);
            }
            else
            {
                es.AddComponent<StandaloneInputModule>();
            }

            Object.DontDestroyOnLoad(es);
        }

        private static RectTransform CreatePanel(Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(740f, 180f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.font = GetRuntimeCjkFont();
            return text;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200f, 60f);

            var text = CreateText(go.transform, name + "Text", Vector2.zero, 24);
            text.text = label;
            text.color = Color.black;
            text.raycastTarget = false;
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            return go;
        }

        private static RectTransform CreateCardGrid(Transform parent)
        {
            var go = new GameObject("ResultCardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -25f);
            rt.sizeDelta = new Vector2(720f, 250f);

            var grid = go.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(200f, 160f);
            grid.spacing = new Vector2(20f, 12f);
            grid.childAlignment = TextAnchor.UpperCenter;
            return rt;
        }

        private static GameObject CreateCardTemplate(Transform parent)
        {
            var card = new GameObject("ResultCardTemplate", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(200f, 160f);
            var image = card.GetComponent<Image>();
            image.color = new Color(0.4f, 0.5f, 0.7f, 0.95f);
            TryApplySprite(image, "GachaTheme/card_bg");

            var frame = card.AddComponent<Outline>();
            frame.effectDistance = new Vector2(2f, 2f);
            frame.effectColor = new Color(1f, 1f, 1f, 0.35f);

            var glow = CreatePanel(card.transform, new Vector2(0.5f, 0.5f), new Vector2(200f, 160f));
            glow.name = "Glow";
            var glowImage = glow.GetComponent<Image>();
            glowImage.color = new Color(1f, 1f, 1f, 0f);
            glowImage.raycastTarget = false;

            var rarityStripe = CreatePanel(card.transform, new Vector2(0.5f, 1f), new Vector2(190f, 22f));
            rarityStripe.name = "RarityStripe";
            var stripeImage = rarityStripe.GetComponent<Image>();
            stripeImage.color = new Color(0.8f, 0.8f, 0.95f, 0.9f);
            rarityStripe.anchoredPosition = new Vector2(0f, -14f);
            stripeImage.raycastTarget = false;

            var portraitFrame = CreatePanel(card.transform, new Vector2(0.5f, 0.5f), new Vector2(172f, 78f));
            portraitFrame.name = "PortraitFrame";
            var portraitFrameImage = portraitFrame.GetComponent<Image>();
            portraitFrameImage.color = new Color(0.07f, 0.09f, 0.18f, 0.72f);
            portraitFrame.anchoredPosition = new Vector2(0f, 12f);
            portraitFrameImage.raycastTarget = false;

            var portrait = CreatePanel(portraitFrame.transform, new Vector2(0.5f, 0.5f), new Vector2(162f, 68f));
            portrait.name = "PortraitImage";
            var portraitImage = portrait.GetComponent<Image>();
            portraitImage.color = new Color(0.38f, 0.5f, 0.74f, 0.96f);
            portraitImage.raycastTarget = false;

            var portraitMark = CreateText(portrait.transform, "PortraitMark", Vector2.zero, 22);
            portraitMark.text = "?";
            portraitMark.color = new Color(1f, 1f, 1f, 0.85f);
            portraitMark.fontStyle = FontStyles.Bold;

            var upTag = CreatePanel(card.transform, new Vector2(0f, 1f), new Vector2(54f, 24f));
            upTag.name = "UpTag";
            var upTagImage = upTag.GetComponent<Image>();
            upTagImage.color = new Color(0.24f, 0.76f, 0.48f, 0.92f);
            upTag.anchoredPosition = new Vector2(30f, -14f);
            upTagImage.raycastTarget = false;
            var upText = CreateText(upTag.transform, "UpTagText", Vector2.zero, 14);
            upText.text = "UP";
            upText.color = Color.white;
            upText.fontStyle = FontStyles.Bold;

            var dupTag = CreatePanel(card.transform, new Vector2(1f, 1f), new Vector2(66f, 24f));
            dupTag.name = "DupTag";
            var dupTagImage = dupTag.GetComponent<Image>();
            dupTagImage.color = new Color(0.84f, 0.35f, 0.35f, 0.9f);
            dupTag.anchoredPosition = new Vector2(-36f, -14f);
            dupTagImage.raycastTarget = false;
            var dupText = CreateText(dupTag.transform, "DupTagText", Vector2.zero, 13);
            dupText.text = "DUP";
            dupText.color = Color.white;
            dupText.fontStyle = FontStyles.Bold;

            var nameText = CreateText(card.transform, "NameText", new Vector2(0f, 24f), 20);
            nameText.rectTransform.sizeDelta = new Vector2(180f, 70f);
            nameText.color = Color.white;
            nameText.fontStyle = FontStyles.Bold;

            var metaText = CreateText(card.transform, "MetaText", new Vector2(0f, -48f), 17);
            metaText.rectTransform.sizeDelta = new Vector2(180f, 44f);
            metaText.color = new Color(0.95f, 0.95f, 1f, 0.96f);

            return card;
        }

        private static void StyleButton(Image image, Color color)
        {
            image.color = color;
        }

        private static void TryApplySprite(Image target, string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                return;
            }

            target.sprite = sprite;
            target.type = Image.Type.Sliced;
        }

        private static TMP_FontAsset GetRuntimeCjkFont()
        {
            if (_runtimeCjkFont != null)
            {
                return _runtimeCjkFont;
            }

            var bundledFont = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (bundledFont != null)
            {
                _runtimeCjkFont = TMP_FontAsset.CreateFontAsset(
                    bundledFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true
                );
                if (_runtimeCjkFont != null)
                {
                    return _runtimeCjkFont;
                }
            }

            var candidates = new[]
            {
                "PingFang SC",
                "Hiragino Sans GB",
                "Heiti SC",
                "Arial Unicode MS",
                "Noto Sans CJK SC"
            };

            Font systemFont = null;
            for (var i = 0; i < candidates.Length; i++)
            {
                systemFont = Font.CreateDynamicFontFromOSFont(candidates[i], 64);
                if (systemFont != null)
                {
                    break;
                }
            }

            if (systemFont == null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            _runtimeCjkFont = TMP_FontAsset.CreateFontAsset(
                systemFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true
            );

            return _runtimeCjkFont != null ? _runtimeCjkFont : TMP_Settings.defaultFontAsset;
        }

        private sealed class SerializedObjectAdapter
        {
            private readonly Object _target;

            public SerializedObjectAdapter(Object target)
            {
                _target = target;
            }

            public void Set<T>(string fieldName, T value) where T : Object
            {
                var type = _target.GetType();
                var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(_target, value);
            }
        }
    }
}
