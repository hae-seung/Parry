using System.Collections.Generic;
using Parry.MonsterClass;
using Parry.PlayerClass;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MonsterAttackPreviewWindow : EditorWindow
{
    private GameObject source;
    private GameObject preview;
    private SpriteRenderer previewRenderer;
    private Vector3 previewPosition;
    private bool hasPreviewPosition;
    private bool movePreview;
    private bool previewFacingLeft;
    private readonly List<ScriptableObject> attacks = new List<ScriptableObject>();
    private readonly BoxBoundsHandle hitboxHandle = new BoxBoundsHandle();
    private ScriptableObject selectedAttack;
    private bool ownsAnimationMode;
    private bool playing;
    private float previewTime;
    private double lastUpdateTime;

    private ObjectField sourceField;
    private DropdownField attackField;
    private Slider timeSlider;
    private Label timeLabel;
    private Label statusLabel;
    private Button playButton;
    private Vector2Field offsetField;
    private Vector2Field sizeField;
    private Vector3Field previewPositionField;

    [MenuItem("Tools/Parry/Attack Preview")]
    public static void Open()
    {
        var window = GetWindow<MonsterAttackPreviewWindow>("공격 판정 미리보기");
        window.minSize = new Vector2(400f, 520f);
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            window.source = selected;
            if (window.sourceField != null)
            {
                window.sourceField.SetValueWithoutNotify(selected);
                window.SetSource(selected);
            }
        }
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        SceneView.duringSceneGui += DrawSceneHandles;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        Undo.undoRedoPerformed += RefreshHitboxFields;
    }

    private void OnDisable()
    {
        StopPreview();
        EditorApplication.update -= UpdatePreview;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SceneView.duringSceneGui -= DrawSceneHandles;
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        Undo.undoRedoPerformed -= RefreshHitboxFields;
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.AddToClassList("attack-preview-root");
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MonsterAttackPreviewWindow.uss");
        if (styleSheet != null)
            rootVisualElement.styleSheets.Add(styleSheet);
        sourceField = new ObjectField("기준 캐릭터") { objectType = typeof(GameObject), allowSceneObjects = true };
        sourceField.RegisterValueChangedCallback(evt => SetSource(evt.newValue as GameObject));
        rootVisualElement.Add(sourceField);
        attackField = new DropdownField("편집할 공격");
        attackField.RegisterValueChangedCallback(_ => SelectAttack(attackField.index));
        rootVisualElement.Add(attackField);

        playButton = new Button(TogglePlayback) { text = "재생" };
        rootVisualElement.Add(playButton);
        rootVisualElement.Add(new Button(() => SetTime(previewTime - FrameDuration)) { text = "이전 프레임" });
        rootVisualElement.Add(new Button(() => SetTime(previewTime + FrameDuration)) { text = "다음 프레임" });
        rootVisualElement.Add(new Button(GoToHitEvent) { text = "타격 이벤트 프레임으로 이동" });
        timeSlider = new Slider("재생 위치", 0f, 1f);
        timeSlider.RegisterValueChangedCallback(evt => SetTime(evt.newValue));
        rootVisualElement.Add(timeSlider);
        timeLabel = new Label();
        rootVisualElement.Add(timeLabel);
        var faceLeft = new Toggle("왼쪽 보기");
        faceLeft.SetValueWithoutNotify(previewFacingLeft);
        faceLeft.RegisterValueChangedCallback(evt =>
        {
            previewFacingLeft = evt.newValue;
            if (previewRenderer != null)
                previewRenderer.flipX = evt.newValue;
            SceneView.RepaintAll();
        });
        rootVisualElement.Add(faceLeft);

        previewPositionField = new Vector3Field("미리보기 캐릭터 위치");
        previewPositionField.RegisterValueChangedCallback(evt => SetPreviewPosition(evt.newValue));
        rootVisualElement.Add(previewPositionField);
        var moveToggle = new Toggle("Scene에서 캐릭터 이동");
        moveToggle.RegisterValueChangedCallback(evt =>
        {
            movePreview = evt.newValue;
            SceneView.RepaintAll();
        });
        rootVisualElement.Add(moveToggle);

        offsetField = new Vector2Field("Hitbox Offset");
        offsetField.RegisterValueChangedCallback(evt => WriteHitbox(evt.newValue, SelectedHitboxSize));
        sizeField = new Vector2Field("Hitbox Size");
        sizeField.RegisterValueChangedCallback(evt => WriteHitbox(SelectedHitboxOffset, evt.newValue));
        rootVisualElement.Add(offsetField);
        rootVisualElement.Add(sizeField);
        rootVisualElement.Add(new Button(() =>
        {
            if (selectedAttack != null)
                AssetDatabase.SaveAssetIfDirty(selectedAttack);
        }) { text = "공격 데이터 저장" });
        var help = new Label("캐릭터 이동을 켜면 Scene 화살표로 미리보기 캐릭터를 옮깁니다. 끄면 공격 판정 위치와 크기를 조절합니다. 판정 변경은 선택한 공격 데이터에 적용됩니다.");
        help.AddToClassList("attack-preview-help");
        rootVisualElement.Add(help);
        statusLabel = new Label();
        rootVisualElement.Add(statusLabel);

        GameObject initial = source;
        if (initial == null && Selection.activeGameObject != null)
            initial = Selection.activeGameObject;
        if (initial == null)
        {
            var monster = Object.FindAnyObjectByType<MonsterBrain>();
            if (monster != null)
                initial = monster.gameObject;
        }
        sourceField.SetValueWithoutNotify(initial);
        SetSource(initial);
    }

    private AnimationClip SelectedClip => selectedAttack is MonsterAttackData monsterAttack
        ? monsterAttack.AnimationClip : (selectedAttack as PlayerAttackData)?.AnimationClip;

    private Vector2 SelectedHitboxOffset => selectedAttack is MonsterAttackData monsterAttack
        ? monsterAttack.HitboxOffset : selectedAttack is PlayerAttackData playerAttack ? playerAttack.HitboxOffset : Vector2.zero;

    private Vector2 SelectedHitboxSize => selectedAttack is MonsterAttackData monsterAttack
        ? monsterAttack.HitboxSize : selectedAttack is PlayerAttackData playerAttack ? playerAttack.HitboxSize : Vector2.zero;

    private float FrameDuration => SelectedClip != null ? 1f / SelectedClip.frameRate : 0.1f;

    private void SetSource(GameObject value)
    {
        StopPreview();
        source = value;
        hasPreviewPosition = false;
        attacks.Clear();
        var names = new List<string>();
        if (source != null && source.TryGetComponent<MonsterBrain>(out var monster) && monster.Data != null)
        {
            foreach (MonsterAttackData attack in monster.Data.Attacks)
            {
                if (attack == null)
                    continue;
                attacks.Add(attack);
                names.Add(attack.name);
            }
        }
        else if (source != null && source.TryGetComponent<PlayerAttack>(out var player))
        {
            // 런타임 공격 컴포넌트에 등록된 목록을 그대로 사용한다.
            var data = new SerializedObject(player);
            foreach (string listName in new[] { "attacks", "berserkerAttacks" })
            {
                var attackList = data.FindProperty(listName);
                for (int i = 0; i < attackList.arraySize; i++)
                {
                    var attack = attackList.GetArrayElementAtIndex(i).objectReferenceValue as PlayerAttackData;
                    if (attack == null)
                        continue;
                    attacks.Add(attack);
                    names.Add(attack.name);
                }
            }
        }
        attackField.choices = names;
        attackField.SetValueWithoutNotify(names.Count > 0 ? names[0] : "");
        SelectAttack(names.Count > 0 ? 0 : -1);
    }

    private void SelectAttack(int index)
    {
        StopPreview();
        selectedAttack = index >= 0 && index < attacks.Count ? attacks[index] : null;
        previewTime = 0f;
        if (selectedAttack == null || SelectedClip == null || source == null)
        {
            statusLabel.text = "씬의 플레이어 또는 몬스터와 공격 클립을 지정해 주세요.";
            RefreshHitboxFields();
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            statusLabel.text = "Play 모드를 종료하면 미리보기를 사용할 수 있습니다.";
            return;
        }

        // 전투 오브젝트와 별도로 만든 임시 스프라이트에만 클립을 샘플링한다.
        var sourceRenderer = source.GetComponent<SpriteRenderer>();
        preview = new GameObject("Attack Hitbox Preview") { hideFlags = HideFlags.HideAndDontSave };
        if (!hasPreviewPosition)
        {
            previewPosition = source.transform.position + Vector3.right * 4f;
            hasPreviewPosition = true;
        }
        preview.transform.position = previewPosition;
        previewPositionField.SetValueWithoutNotify(previewPosition);
        preview.transform.rotation = source.transform.rotation;
        preview.transform.localScale = source.transform.lossyScale;
        previewRenderer = preview.AddComponent<SpriteRenderer>();
        previewRenderer.sprite = sourceRenderer.sprite;
        previewRenderer.flipX = previewFacingLeft;
        previewRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        previewRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        previewRenderer.sortingOrder = sourceRenderer.sortingOrder;
        var animator = preview.AddComponent<Animator>();
        animator.runtimeAnimatorController = source.GetComponent<Animator>().runtimeAnimatorController;
        animator.fireEvents = false;

        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();
        AnimationMode.StartAnimationMode();
        ownsAnimationMode = true;
        timeSlider.highValue = SelectedClip.length;
        statusLabel.text = "선택한 공격 하나를 미리 보고 있습니다.";
        Sample();
        RefreshHitboxFields();
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.Frame(new Bounds(preview.transform.position, Vector3.one * 4f), false);
        }
    }

    private void TogglePlayback()
    {
        if (preview == null)
            return;
        if (previewTime >= SelectedClip.length)
            previewTime = 0f;
        playing = !playing;
        lastUpdateTime = EditorApplication.timeSinceStartup;
        playButton.text = playing ? "일시정지" : "재생";
    }

    private void SetTime(float time)
    {
        playing = false;
        playButton.text = "재생";
        if (SelectedClip == null)
            return;
        previewTime = Mathf.Clamp(Mathf.Round(time / FrameDuration) * FrameDuration, 0f, SelectedClip.length);
        Sample();
    }

    private void GoToHitEvent()
    {
        if (SelectedClip == null)
            return;
        foreach (AnimationEvent animationEvent in AnimationUtility.GetAnimationEvents(SelectedClip))
        {
            if (animationEvent.functionName == nameof(MonsterAttack.ApplyAttackHit))
            {
                SetTime(animationEvent.time);
                return;
            }
        }
        statusLabel.text = "ApplyAttackHit 이벤트가 없습니다.";
    }

    private void UpdatePreview()
    {
        if (!playing || preview == null || selectedAttack == null)
            return;
        double now = EditorApplication.timeSinceStartup;
        previewTime = Mathf.Min(previewTime + (float)(now - lastUpdateTime), SelectedClip.length);
        lastUpdateTime = now;
        if (previewTime >= SelectedClip.length)
        {
            playing = false;
            playButton.text = "재생";
        }
        Sample();
    }

    private void Sample()
    {
        if (preview == null || !ownsAnimationMode || !AnimationMode.InAnimationMode())
            return;
        AnimationMode.BeginSampling();
        try
        {
            AnimationMode.SampleAnimationClip(preview, SelectedClip, previewTime);
        }
        finally
        {
            AnimationMode.EndSampling();
        }
        timeSlider.SetValueWithoutNotify(previewTime);
        // 프레임을 바꾸거나 재생해도 사용자가 배치한 위치를 유지한다.
        preview.transform.position = previewPosition;
        timeLabel.text = $"{previewTime:0.000}초 / {SelectedClip.length:0.000}초 · {Mathf.RoundToInt(previewTime / FrameDuration)} 프레임";
        SceneView.RepaintAll();
    }

    private void DrawSceneHandles(SceneView sceneView)
    {
        if (preview == null || selectedAttack == null || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        Vector3 origin = preview.transform.position;
        int direction = previewRenderer.flipX ? -1 : 1;
        Vector3 center = GetHitboxCenter();
        hitboxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        hitboxHandle.center = center;
        hitboxHandle.size = new Vector3(SelectedHitboxSize.x, SelectedHitboxSize.y, 0f);
        hitboxHandle.SetColor(Color.cyan);
        Handles.Label(origin + Vector3.up * 1.5f, selectedAttack.name);
        if (movePreview)
        {
            Color previousColor = Handles.color;
            Handles.color = Color.cyan;
            Handles.DrawWireCube(center, hitboxHandle.size);
            Handles.color = previousColor;
            EditorGUI.BeginChangeCheck();
            Vector3 movedPosition = Handles.PositionHandle(preview.transform.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
                SetPreviewPosition(movedPosition);
            return;
        }

        EditorGUI.BeginChangeCheck();
        hitboxHandle.DrawHandle();
        Vector3 movedCenter = Handles.PositionHandle(hitboxHandle.center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Vector3 delta = movedCenter - origin;
            WriteHitbox(new Vector2(delta.x * direction, delta.y), hitboxHandle.size);
        }
    }

    private Vector3 GetHitboxCenter()
    {
        Vector2 offset = SelectedHitboxOffset;
        int direction = previewRenderer.flipX ? -1 : 1;
        return preview.transform.position + new Vector3(offset.x * direction, offset.y, 0f);
    }

    private void SetPreviewPosition(Vector3 position)
    {
        previewPosition = position;
        hasPreviewPosition = true;
        if (preview != null)
            preview.transform.position = position;
        previewPositionField.SetValueWithoutNotify(position);
        SceneView.RepaintAll();
    }

    private void WriteHitbox(Vector2 offset, Vector2 size)
    {
        if (selectedAttack == null)
            return;
        var data = new SerializedObject(selectedAttack);
        data.FindProperty("hitboxOffset").vector2Value = offset;
        data.FindProperty("hitboxSize").vector2Value = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        data.ApplyModifiedProperties();
        RefreshHitboxFields();
    }

    private void RefreshHitboxFields()
    {
        if (offsetField == null || sizeField == null)
            return;
        offsetField.SetEnabled(selectedAttack != null);
        sizeField.SetEnabled(selectedAttack != null);
        if (selectedAttack != null)
        {
            offsetField.SetValueWithoutNotify(SelectedHitboxOffset);
            sizeField.SetValueWithoutNotify(SelectedHitboxSize);
        }
        SceneView.RepaintAll();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            StopPreview();
    }

    private void StopPreview()
    {
        playing = false;
        if (playButton != null)
            playButton.text = "재생";
        if (ownsAnimationMode && AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();
        ownsAnimationMode = false;
        if (preview != null)
            DestroyImmediate(preview);
        preview = null;
        previewRenderer = null;
        SceneView.RepaintAll();
    }
}
