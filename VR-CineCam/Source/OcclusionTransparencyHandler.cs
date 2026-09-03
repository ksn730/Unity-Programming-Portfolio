using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class OcclusionTransparencyHandler : MonoBehaviour
{
    [Header("References")]
    public ShotEvaluator shotEvaluator;
    public CinemachineBrain filmBrain;

    [Header("Transparency Settings")]
    [Range(0f, 1f)] public float targetAlpha = 0.25f;
    public float fadeSpeed = 8f;
    public LayerMask occlusionMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Exclusions")]
    public string[] excludeTags = new string[] { "Player", "Enemy", "Dead" };

    [Header("Camera Passthrough")]
    [Tooltip("카메라가 오브젝트 내부에 들어갔을 때 감지할 반경 (m)")]
    public float cameraOverlapRadius = 0.15f;

    [Header("Near-Field Detection")]
    [Tooltip("카메라 전방 근접 오브젝트 감지 구체 반경 (m). 카메라 바로 앞 건물 투명화.")]
    public float nearFieldRadius = 0.5f;
    [Tooltip("카메라 전방 근접 감지 거리 (m).")]
    public float nearFieldDistance = 2.5f;

    // ── Build Shader Variant Templates ───────────────────────────────────────
    // 빌드에서 셰이더 변형 스트리핑을 막기 위해 반드시 할당해야 합니다.
    // 방법: Project 창에서 Create > Material 두 개 만들고
    //   - urpLitTransparentTemplate  : URP/Lit,  Surface Type = Transparent
    //   - urpUnlitTransparentTemplate: URP/Unlit, Surface Type = Transparent
    // 로 설정한 뒤 Inspector에 드래그.
    [Header("Build - Shader Variant Templates (빌드 필수)")]
    [Tooltip("Surface Type = Transparent 인 URP Lit 재질. 빌드에서 셰이더 변형 포함 보장.")]
    public Material urpLitTransparentTemplate;
    [Tooltip("Surface Type = Transparent 인 URP Unlit 재질 (폴백용). 빌드에서 셰이더 변형 포함 보장.")]
    public Material urpUnlitTransparentTemplate;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public int detectedOccluderCount = 0;

    private class OccluderState
    {
        public Renderer renderer;
        public Material[] originalMats;  // 원본 sharedMaterial 레퍼런스
        public Material[] instanceMats;  // 이 Renderer 전용 인스턴스 재질
        public float currentAlpha = 1f;
        public bool isBlocking;
        public bool wasStaticBatched;
    }

    private readonly Dictionary<Renderer, OccluderState> _states = new Dictionary<Renderer, OccluderState>();
    private readonly HashSet<Renderer> _blockingThisFrame = new HashSet<Renderer>();
    private readonly HashSet<GameObject> _transparentObjects = new HashSet<GameObject>();

    public bool IsCurrentlyTransparent(GameObject go) => _transparentObjects.Contains(go);

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    // VR 카메라 렌더링 직전: 원본 재질로 임시 복원 (VR 플레이어는 투명하지 않게 보임)
    private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        // 필름 카메라는 항상 투명 재질을 보아야 하므로 제외.
        // 빌드에서 stereoEnabled가 true여도 스왑하지 않음.
        if (filmBrain != null && cam == filmBrain.OutputCamera) return;
        if (!cam.stereoEnabled) return;
        foreach (var kv in _states)
        {
            var s = kv.Value;
            if (s.renderer == null) continue;
            if (s.wasStaticBatched)
                s.renderer.materials = s.originalMats;
            else
                s.renderer.sharedMaterials = s.originalMats;
        }
    }

    // VR 카메라 렌더링 완료 후: 투명 재질 재적용 (다음 필름 카메라용)
    private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (filmBrain != null && cam == filmBrain.OutputCamera) return;
        if (!cam.stereoEnabled) return;
        foreach (var kv in _states)
        {
            var s = kv.Value;
            if (s.renderer == null) continue;
            if (s.wasStaticBatched)
                s.renderer.materials = s.instanceMats;
            else
                s.renderer.sharedMaterials = s.instanceMats;
        }
    }

    private void LateUpdate()
    {
        if (shotEvaluator == null) return;
        if (filmBrain == null) filmBrain = FindObjectOfType<CinemachineBrain>();
        if (filmBrain == null) return;

        Vector3 camPos = filmBrain.transform.position;

        _blockingThisFrame.Clear();
        _transparentObjects.Clear();

        if (shotEvaluator.playerRoot != null)
        {
            foreach (var samplePos in shotEvaluator.GetPlayerSamplePositions())
            {
                CastRay(camPos, samplePos, shotEvaluator.playerRoot.gameObject);
                CastRay(samplePos, camPos, shotEvaluator.playerRoot.gameObject);
            }
        }

        if (shotEvaluator.attentionTargetEstimator != null)
        {
            AttentionTargetState attn = shotEvaluator.attentionTargetEstimator.CurrentAttention;
            if (attn.isValid && attn.targetObject != null)
            {
                foreach (var offset in shotEvaluator.targetSampleOffsets)
                {
                    Vector3 targetSamplePos = attn.targetObject.transform.position + offset;
                    CastRay(camPos, targetSamplePos, attn.targetObject);
                    CastRay(targetSamplePos, camPos, attn.targetObject);
                }
            }
        }

        // 카메라가 오브젝트 내부에 완전히 들어간 경우 OverlapSphere로 감지
        OverlapAtCamera(camPos);

        // 카메라 바로 앞 근접 건물 감지 (레이캐스트 미검출 케이스)
        NearFieldDetection(camPos, filmBrain.transform.forward);

        detectedOccluderCount = _blockingThisFrame.Count;
        UpdateFade();
    }

    private void NearFieldDetection(Vector3 camPos, Vector3 camForward)
    {
        if (nearFieldRadius <= 0f || nearFieldDistance <= 0f) return;

        RaycastHit[] hits = Physics.SphereCastAll(
            camPos, nearFieldRadius, camForward, nearFieldDistance,
            occlusionMask, triggerInteraction);

        GameObject playerGo = shotEvaluator.playerRoot != null ? shotEvaluator.playerRoot.gameObject : null;
        GameObject targetGo = null;
        if (shotEvaluator.attentionTargetEstimator != null)
        {
            AttentionTargetState attn = shotEvaluator.attentionTargetEstimator.CurrentAttention;
            if (attn.isValid) targetGo = attn.targetObject;
        }

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            GameObject go = hit.collider.gameObject;

            if (playerGo != null && (go == playerGo || hit.collider.transform.IsChildOf(playerGo.transform))) continue;
            if (targetGo != null && (go == targetGo || hit.collider.transform.IsChildOf(targetGo.transform))) continue;

            bool excluded = false;
            foreach (var tag in excludeTags)
                if (!string.IsNullOrEmpty(tag) && go.CompareTag(tag)) { excluded = true; break; }
            if (excluded) continue;

            Renderer[] renderers = GetRenderersOfOccluder(go);
            foreach (var r in renderers)
            {
                _blockingThisFrame.Add(r);
                _transparentObjects.Add(r.gameObject);
            }
            _transparentObjects.Add(go);
        }
    }

    private void OverlapAtCamera(Vector3 camPos)
    {
        Collider[] cols = Physics.OverlapSphere(camPos, cameraOverlapRadius, occlusionMask, triggerInteraction);
        foreach (var col in cols)
        {
            if (col == null) continue;
            GameObject go = col.gameObject;

            bool excluded = false;
            foreach (var tag in excludeTags)
                if (!string.IsNullOrEmpty(tag) && go.CompareTag(tag)) { excluded = true; break; }
            if (excluded) continue;

            Renderer[] renderers = GetRenderersOfOccluder(go);
            foreach (var r in renderers)
            {
                _blockingThisFrame.Add(r);
                _transparentObjects.Add(r.gameObject);
            }
            _transparentObjects.Add(go);
        }
    }

    private void CastRay(Vector3 from, Vector3 to, GameObject ignore)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 1e-4f) return;
        dir /= dist;

        RaycastHit[] hits = Physics.RaycastAll(from, dir, dist, occlusionMask, triggerInteraction);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            GameObject go = hit.collider.gameObject;
            if (go == ignore || hit.collider.transform.IsChildOf(ignore.transform)) continue;

            bool excluded = false;
            foreach (var tag in excludeTags)
                if (!string.IsNullOrEmpty(tag) && go.CompareTag(tag)) { excluded = true; break; }
            if (excluded) continue;

            if (showDebugLogs) Debug.Log("[OcclusionHandler] Hit: " + go.name);

            Renderer[] renderers = GetRenderersOfOccluder(go);
            foreach (var r in renderers)
            {
                _blockingThisFrame.Add(r);
                _transparentObjects.Add(r.gameObject);
            }
            _transparentObjects.Add(go);
        }
    }

    private static Renderer[] GetRenderersOfOccluder(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0) return renderers;

        // 콜라이더가 자식에 있고 렌더러가 부모에 있는 경우 위로 탐색
        Transform t = go.transform.parent;
        while (t != null)
        {
            renderers = t.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0) return renderers;
            t = t.parent;
        }

        return System.Array.Empty<Renderer>();
    }

    private void UpdateFade()
    {
        foreach (var r in _blockingThisFrame)
        {
            if (!_states.TryGetValue(r, out OccluderState s))
            {
                s = BeginFade(r);
                if (s == null) continue;
                _states[r] = s;
            }
            s.isBlocking = true;
        }

        var toRemove = new List<Renderer>();
        foreach (var kv in _states)
        {
            OccluderState s = kv.Value;
            float goal = s.isBlocking ? targetAlpha : 1f;
            s.currentAlpha = Mathf.MoveTowards(s.currentAlpha, goal, fadeSpeed * Time.deltaTime);
            ApplyAlpha(s);

            if (!s.isBlocking && s.currentAlpha >= 1f)
            {
                Revert(s);
                toRemove.Add(kv.Key);
            }
            s.isBlocking = false;
        }
        foreach (var key in toRemove) _states.Remove(key);
    }

    private OccluderState BeginFade(Renderer r)
    {
        if (r == null) return null;

        bool isStaticBatched = r.isPartOfStaticBatch;
        Material[] originals = r.sharedMaterials;
        Material[] instances = new Material[originals.Length];

        for (int i = 0; i < originals.Length; i++)
        {
            if (originals[i] == null) { instances[i] = null; continue; }

            instances[i] = CreateTransparentInstance(originals[i]);
        }

        // Static-batched 렌더러는 sharedMaterials 교체가 시각적으로 반영되지 않으므로
        // materials 프로퍼티를 사용해 퍼-렌더러 인스턴스로 배칭을 일시 해제하고 투명화
        if (isStaticBatched)
            r.materials = instances;
        else
            r.sharedMaterials = instances;

        if (showDebugLogs)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[OcclusionHandler] BeginFade: {r.gameObject.name} | mats={originals.Length}");
            for (int dbg = 0; dbg < originals.Length; dbg++)
            {
                if (originals[dbg] == null) { sb.Append(" [null]"); continue; }
                sb.Append($" | [{dbg}] shader={originals[dbg].shader.name}");
                sb.Append($" hasSurface={originals[dbg].HasProperty("_Surface")}");
                sb.Append($" hasColor={originals[dbg].HasProperty("_Color")}");
            }
            Debug.Log(sb.ToString());
        }

        return new OccluderState
        {
            renderer = r,
            originalMats = originals,
            instanceMats = instances,
            currentAlpha = 1f,
            wasStaticBatched = isStaticBatched
        };
    }

    // 원본 재질에서 투명 인스턴스를 생성합니다.
    // 템플릿(에셋)이 할당된 경우: 미리 컴파일된 셰이더 변형을 사용 → 에디터/빌드 모두 안전.
    // 미할당인 경우: 런타임 EnableKeyword 방식 → URP 버전에 따라 동작하지 않을 수 있음.
    private Material CreateTransparentInstance(Material orig)
    {
        // Lit 계열(Lit/Simple Lit)은 Lit 템플릿 우선, 나머지는 Unlit 템플릿 우선
        bool isLit = orig.shader != null &&
                     (orig.shader.name.Contains("Lit") && !orig.shader.name.Contains("Unlit"));
        Material template = isLit
            ? (urpLitTransparentTemplate  ?? urpUnlitTransparentTemplate)
            : (urpUnlitTransparentTemplate ?? urpLitTransparentTemplate);

        if (template != null)
        {
            Material inst = new Material(template);
            CopyBaseProperties(orig, inst);
            return inst;
        }

        // 템플릿 미할당: 경고 후 폴백 (URP 버전에 따라 동작 불안정)
        if (showDebugLogs)
            Debug.LogWarning("[OcclusionHandler] 투명 템플릿 재질 미할당. " +
                "컴포넌트 우클릭 → [Auto-Create Transparent Templates] 실행 필요.");

        if (orig.HasProperty("_Surface") || orig.HasProperty("_Color"))
        {
            Material inst = new Material(orig);
            EnableTransparency(inst);
            return inst;
        }

        Material fallback = CreateFallbackMaterial(orig);
        return fallback ?? new Material(orig);
    }

#if UNITY_EDITOR
    // Inspector에서 우클릭 → 메뉴 실행 시 투명 템플릿 재질을 Assets 폴더에 자동 생성
    [UnityEditor.MenuItem("CONTEXT/OcclusionTransparencyHandler/Auto-Create Transparent Templates")]
    private static void AutoCreateTransparentTemplatesMenu(UnityEditor.MenuCommand cmd)
    {
        var handler = cmd.context as OcclusionTransparencyHandler;
        if (handler != null) handler.AutoCreateTransparentTemplates();
    }

    [ContextMenu("Auto-Create Transparent Templates")]
    private void AutoCreateTransparentTemplates()
    {
        string dir = "Assets/";

        if (urpLitTransparentTemplate == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null)
            {
                var mat = new Material(s) { name = "OcclusionFade_Lit" };
                EnableTransparency(mat);
                mat.SetColor("_BaseColor", Color.white);
                UnityEditor.AssetDatabase.CreateAsset(mat, dir + "OcclusionFade_Lit.mat");
                urpLitTransparentTemplate = mat;
                Debug.Log("[OcclusionHandler] OcclusionFade_Lit.mat 생성됨");
            }
        }

        if (urpUnlitTransparentTemplate == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null)
            {
                var mat = new Material(s) { name = "OcclusionFade_Unlit" };
                EnableTransparency(mat);
                mat.SetColor("_BaseColor", Color.white);
                UnityEditor.AssetDatabase.CreateAsset(mat, dir + "OcclusionFade_Unlit.mat");
                urpUnlitTransparentTemplate = mat;
                Debug.Log("[OcclusionHandler] OcclusionFade_Unlit.mat 생성됨");
            }
        }

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[OcclusionHandler] 템플릿 생성 완료. 씬을 저장하세요.");
    }
#endif

    // URP 재질의 주요 시각 속성을 복사 (텍스처, 색상, 타일링)
    private static void CopyBaseProperties(Material src, Material dst)
    {
        // 메인 텍스처
        if (src.HasProperty("_BaseMap") && dst.HasProperty("_BaseMap"))
        {
            dst.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
            dst.SetTextureScale("_BaseMap", src.GetTextureScale("_BaseMap"));
            dst.SetTextureOffset("_BaseMap", src.GetTextureOffset("_BaseMap"));
        }
        else if (src.mainTexture != null)
        {
            dst.mainTexture = src.mainTexture;
            dst.mainTextureScale = src.mainTextureScale;
            dst.mainTextureOffset = src.mainTextureOffset;
        }

        // 기본 색상 (알파는 ApplyAlpha가 제어하므로 1로 유지)
        if (src.HasProperty("_BaseColor") && dst.HasProperty("_BaseColor"))
        {
            Color c = src.GetColor("_BaseColor");
            c.a = 1f;
            dst.SetColor("_BaseColor", c);
        }

        // 노말 맵
        if (src.HasProperty("_BumpMap") && dst.HasProperty("_BumpMap"))
            dst.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));

        // 이미시브
        if (src.HasProperty("_EmissionMap") && dst.HasProperty("_EmissionMap"))
            dst.SetTexture("_EmissionMap", src.GetTexture("_EmissionMap"));
        if (src.HasProperty("_EmissionColor") && dst.HasProperty("_EmissionColor"))
            dst.SetColor("_EmissionColor", src.GetColor("_EmissionColor"));
    }

    // 폴백 재질용: 텍스처만 복사
    private static void CopyBaseTextureOnly(Material src, Material dst)
    {
        Texture tex = src.mainTexture;
        if (tex == null) return;

        if (dst.HasProperty("_BaseMap"))
        {
            dst.SetTexture("_BaseMap", tex);
            dst.SetTextureScale("_BaseMap", src.mainTextureScale);
            dst.SetTextureOffset("_BaseMap", src.mainTextureOffset);
        }
        if (dst.HasProperty("_MainTex"))
        {
            dst.SetTexture("_MainTex", tex);
            dst.SetTextureScale("_MainTex", src.mainTextureScale);
            dst.SetTextureOffset("_MainTex", src.mainTextureOffset);
        }
    }

    // Shader.Find 기반 폴백 (에디터 또는 템플릿 미할당 시)
    private static Material CreateFallbackMaterial(Material orig)
    {
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) return null;

        Material inst = new Material(urpUnlit);
        CopyBaseTextureOnly(orig, inst);
        inst.SetColor("_BaseColor", Color.white);
        EnableTransparency(inst);
        return inst;
    }

    private static bool HasTransparencySupport(Material mat)
    {
        return mat.HasProperty("_Surface") || mat.HasProperty("_Color");
    }

    private static void EnableTransparency(Material mat)
    {
        // URP Lit / Unlit / Simple Lit
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_SrcBlendAlpha"))
                mat.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha"))
                mat.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_AlphaToMask"))
                mat.SetFloat("_AlphaToMask", 0f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);
            mat.SetInt("_ZWrite", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
        // Built-in Standard
        else if (mat.HasProperty("_Color"))
        {
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }

    private void ApplyAlpha(OccluderState s)
    {
        if (s.renderer == null) return;
        Material[] mats = s.renderer.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            if (mats[i].HasProperty("_BaseColor"))
            {
                Color c = mats[i].GetColor("_BaseColor");
                c.a = s.currentAlpha;
                mats[i].SetColor("_BaseColor", c);
            }
            else if (mats[i].HasProperty("_Color"))
            {
                Color c = mats[i].GetColor("_Color");
                c.a = s.currentAlpha;
                mats[i].SetColor("_Color", c);
            }
        }
    }

    private void Revert(OccluderState s)
    {
        if (s.renderer != null)
        {
            if (s.wasStaticBatched)
                s.renderer.materials = s.originalMats;
            else
                s.renderer.sharedMaterials = s.originalMats;
        }

        foreach (var m in s.instanceMats)
            if (m != null) Destroy(m);

        if (showDebugLogs) Debug.Log("[OcclusionHandler] Reverted: " + s.renderer?.gameObject.name);
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        foreach (var kv in _states) Revert(kv.Value);
        _states.Clear();
    }
}
