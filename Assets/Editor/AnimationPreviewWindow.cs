using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class AnimationPreviewWindow : EditorWindow
{
    private GameObject source;
    private AnimationClip clip;
    private GameObject preview;
    private Vector3 previewPosition;
    private bool playing;
    private float time;
    private double lastUpdateTime;
    private Slider timeSlider;
    private Label status;
    private Button playButton;

    [MenuItem("Tools/Parry/Animation Preview")]
    public static void Open()
    {
        GetWindow<AnimationPreviewWindow>("애니메이션 미리보기").minSize = new Vector2(360f, 240f);
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
    }

    private void OnDisable()
    {
        StopPreview();
        EditorApplication.update -= UpdatePreview;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        var sourceField = new ObjectField("오브젝트")
        {
            objectType = typeof(GameObject), allowSceneObjects = true
        };
        sourceField.SetValueWithoutNotify(source);
        sourceField.RegisterValueChangedCallback(evt =>
        {
            source = evt.newValue as GameObject;
            CreatePreview();
        });
        rootVisualElement.Add(sourceField);

        var clipField = new ObjectField("애니메이션 클립")
        {
            objectType = typeof(AnimationClip), allowSceneObjects = false
        };
        clipField.SetValueWithoutNotify(clip);
        clipField.RegisterValueChangedCallback(evt =>
        {
            clip = evt.newValue as AnimationClip;
            CreatePreview();
        });
        rootVisualElement.Add(clipField);
        playButton = new Button(() =>
        {
            if (preview == null) return;
            if (time >= clip.length) time = 0f;
            playing = !playing;
            lastUpdateTime = EditorApplication.timeSinceStartup;
            playButton.text = playing ? "일시정지" : "재생";
        }) { text = "재생" };
        rootVisualElement.Add(playButton);
        rootVisualElement.Add(new Button(() => SetTime(time - FrameDuration)) { text = "이전 프레임" });
        rootVisualElement.Add(new Button(() => SetTime(time + FrameDuration)) { text = "다음 프레임" });
        timeSlider = new Slider("재생 위치", 0f, 1f);
        timeSlider.RegisterValueChangedCallback(evt => SetTime(evt.newValue));
        rootVisualElement.Add(timeSlider);
        status = new Label();
        rootVisualElement.Add(status);
        CreatePreview();
    }

    private float FrameDuration => clip != null && clip.frameRate > 0f ? 1f / clip.frameRate : 0.1f;

    private void CreatePreview()
    {
        StopPreview();
        time = 0f;
        if (source == null || clip == null)
        {
            status.text = "오브젝트와 애니메이션 클립을 지정해 주세요.";
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            status.text = "Play 모드를 종료한 뒤 사용할 수 있습니다.";
            return;
        }

        // 실행 컴포넌트 없이 스프라이트 계층만 복제해 원본과 떨어진 위치에서 재생한다.
        preview = CopySpriteHierarchy(source.transform);
        if (!preview.TryGetComponent<Animator>(out _))
            preview.AddComponent<Animator>().fireEvents = false;
        preview.name = "Animation Preview";
        previewPosition = source.transform.position + Vector3.right * 4f;
        preview.transform.position = previewPosition;
        preview.transform.rotation = source.transform.rotation;
        preview.transform.localScale = source.transform.lossyScale;
        timeSlider.highValue = clip.length;
        Sample();
        SceneView.lastActiveSceneView?.Frame(new Bounds(previewPosition, Vector3.one * 4f), false);
    }

    private static GameObject CopySpriteHierarchy(Transform original)
    {
        var copy = new GameObject(original.name) { hideFlags = HideFlags.HideAndDontSave };
        copy.transform.localPosition = original.localPosition;
        copy.transform.localRotation = original.localRotation;
        copy.transform.localScale = original.localScale;
        if (original.TryGetComponent<SpriteRenderer>(out var renderer))
        {
            var copiedRenderer = copy.AddComponent<SpriteRenderer>();
            EditorUtility.CopySerialized(renderer, copiedRenderer);
        }
        if (original.TryGetComponent<Animator>(out var animator))
        {
            var copiedAnimator = copy.AddComponent<Animator>();
            copiedAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
            copiedAnimator.fireEvents = false;
        }
        for (int i = 0; i < original.childCount; i++)
            CopySpriteHierarchy(original.GetChild(i)).transform.SetParent(copy.transform, false);
        copy.SetActive(original.gameObject.activeSelf);
        return copy;
    }

    private void SetTime(float value)
    {
        playing = false;
        playButton.text = "재생";
        if (preview == null) return;
        time = Mathf.Clamp(Mathf.Round(value / FrameDuration) * FrameDuration, 0f, clip.length);
        Sample();
    }

    private void UpdatePreview()
    {
        if (!playing || preview == null || clip == null) return;
        double now = EditorApplication.timeSinceStartup;
        time = Mathf.Min(time + (float)(now - lastUpdateTime), clip.length);
        lastUpdateTime = now;
        if (time >= clip.length)
        {
            playing = false;
            playButton.text = "재생";
        }
        Sample();
    }

    private void Sample()
    {
        // 임시 오브젝트에 직접 샘플링해 다른 미리보기 창의 AnimationMode와 독립적으로 동작한다.
        clip.SampleAnimation(preview, time);
        preview.transform.position = previewPosition;
        timeSlider.SetValueWithoutNotify(time);
        status.text = $"{time:0.000}초 / {clip.length:0.000}초";
        SceneView.RepaintAll();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) StopPreview();
    }

    private void StopPreview()
    {
        playing = false;
        if (playButton != null) playButton.text = "재생";
        if (preview != null) DestroyImmediate(preview);
        preview = null;
    }
}
