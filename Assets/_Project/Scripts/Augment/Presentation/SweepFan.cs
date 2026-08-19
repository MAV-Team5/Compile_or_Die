using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부채꼴을 메시로 직접 그린다. 각도를 넓히면 파이 차트처럼 채워진다.
///
/// UI Image 의 Radial Fill 과 같은 일을 Canvas 없이 한다 —
/// Canvas 는 채움 값이 매 프레임 바뀌면 통째로 리빌드돼서 오히려 더 비싸고,
/// SpriteRenderer 들과 정렬 순서가 따로 논다.
///
/// 정점 컬러를 쓰므로 "방금 지난 쪽은 밝고 오래된 쪽은 옅게"가 공짜로 나온다.
/// 매 프레임 배열을 새로 잡지 않으므로 GC 할당은 0이다.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SweepFan : MonoBehaviour
{
    [Header("해상도")]
    [Tooltip("부채꼴을 몇 조각으로 쪼갤지. 48이면 충분히 매끄럽다.\n" +
             "삼각형 수가 곧 이 값이라 올려도 거의 공짜지만, 24 아래로 내리면 각져 보인다.")]
    [Range(8, 128)] public int segments = 48;

    [Header("그림")]
    [Tooltip("잔상에 입힐 그림. 비우면 아래 색만으로 단색 그라디언트가 된다.\n" +
             "정사각 안에 꽉 차는 원판으로 그릴 것 — 가운데가 부채꼴 꼭짓점이 된다.\n" +
             "스프라이트 시트에서 잘라 쓴 것도 알아서 맞춰준다.")]
    public Sprite sprite;

    [Header("색")]
    [Tooltip("머티리얼에 텍스처를 넣었으면 아래 세 색은 거기에 곱해진다.\n" +
             "원본 색감을 그대로 쓰려면 셋 다 흰색으로 두고 알파만 조절할 것.")]
    public Color nearColor = new(0.4f, 1f, 0.7f, 0.10f);

    [Tooltip("바깥쪽, 스캔 라인 바로 뒤. 방금 지나간 자리라 가장 밝다.")]
    public Color leadColor = new(0.4f, 1f, 0.7f, 0.45f);

    [Tooltip("바깥쪽, 꼬리 끝. 오래전에 지나간 자리라 옅다.")]
    public Color tailColor = new(0.4f, 1f, 0.7f, 0.05f);

    [Header("정렬")]
    [Tooltip("스프라이트와 같은 정렬 레이어 이름. 비우면 Default.")]
    public string sortingLayer = "Default";

    [Tooltip("같은 레이어 안에서의 순서. 적보다 낮게 둬야 바닥에 깔린다.")]
    public int sortingOrder = -1;

    Mesh mesh;
    MeshRenderer meshRenderer;

    // 매 프레임 재사용한다. 여기서 new 를 하면 GC 가 돈다
    readonly List<Vector3> vertices = new();
    readonly List<Color> colors = new();
    readonly List<Vector2> uvs = new();
    readonly List<int> triangles = new();

    int builtSegments = -1;
    bool builtReversed;
    float alpha = 1f;

    /// <summary>머티리얼을 안 물렸을 때 쓸 대타. 인스턴스마다 만들면 새기 때문에 하나만 둔다.</summary>
    static Material fallbackMaterial;

    static readonly int MainTex = Shader.PropertyToID("_MainTex");

    /// <summary>텍스처 안에서 이 그림이 차지하는 영역. 시트에서 잘라 쓴 조각을 위해 필요하다.</summary>
    Rect uvRect = new(0f, 0f, 1f, 1f);

    void Awake()
    {
        mesh = new Mesh { name = "SweepFan" };
        GetComponent<MeshFilter>().mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();

        // MeshRenderer 의 정렬은 인스펙터에서 잘 안 보여서 코드로 잡아준다.
        // 안 잡으면 스프라이트 뒤에 숨거나 앞으로 튀어나온다
        meshRenderer.sortingLayerName = string.IsNullOrEmpty(sortingLayer) ? "Default" : sortingLayer;
        meshRenderer.sortingOrder = sortingOrder;

        EnsureMaterial();
        ApplySprite();
        Clear();
    }

    /// <summary>
    /// 스프라이트를 머티리얼 대신 프로퍼티 블록으로 물린다.
    /// 머티리얼에 직접 넣으면 인스턴스가 하나씩 복제되고, 무엇보다
    /// 스프라이트와 텍스처는 다른 에셋이라 인스펙터에서 드래그가 거부당한다.
    /// </summary>
    void ApplySprite()
    {
        if (sprite == null || sprite.texture == null)
        {
            uvRect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        // 스프라이트 시트에서 잘라 쓴 조각이면 텍스처 안의 제 자리만 써야 한다
        Rect pixels = sprite.textureRect;
        float width = sprite.texture.width;
        float height = sprite.texture.height;

        uvRect = new Rect(pixels.x / width, pixels.y / height,
                          pixels.width / width, pixels.height / height);

        var block = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(block);
        block.SetTexture(MainTex, sprite.texture);
        meshRenderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// 머티리얼을 깜빡해도 일단 보이게 한다.
    /// 빈 채로 두면 아무것도 안 나와서 "왜 안 보이지"로 한참 헤매게 된다.
    /// </summary>
    void EnsureMaterial()
    {
        if (meshRenderer.sharedMaterial != null) return;

        if (fallbackMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning($"[{name}] SweepFan 에 머티리얼이 없고 대타 셰이더도 못 찾았습니다. " +
                                 "MeshRenderer 에 Sprites/Default 머티리얼을 물려주세요", this);
                return;
            }

            fallbackMaterial = new Material(shader) { name = "SweepFan Fallback" };
        }

        meshRenderer.sharedMaterial = fallbackMaterial;

        Debug.LogWarning($"[{name}] SweepFan 에 머티리얼이 없어 기본 스프라이트 머티리얼로 대신합니다. " +
                         "직접 만들어 물리면 색·블렌딩을 조절할 수 있습니다", this);
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    /// <summary>전체 투명도 배수. 페이드아웃에 쓴다.</summary>
    public void SetAlpha(float value) => alpha = Mathf.Clamp01(value);

    /// <summary>아무것도 안 보이게 비운다.</summary>
    public void Clear() => mesh.Clear();

    /// <summary>
    /// 부채꼴을 다시 그린다.
    /// startDegrees 에서 시작해 sweptDegrees 만큼 벌어진다 — 음수면 시계 방향.
    /// 꼬리 끝이 tailColor, 벌어진 끝이 leadColor 다.
    ///
    /// artAngle 은 텍스처가 놓인 방향이다. 그림이 원판 한가운데를 채우고 있다고 보고
    /// 부채꼴이 그 원판의 해당 각도 조각만 잘라 보여준다.
    /// </summary>
    public void Draw(float radius, float startDegrees, float sweptDegrees, float artAngle = 0f)
    {
        if (radius <= 0f || Mathf.Abs(sweptDegrees) < 0.01f)
        {
            Clear();
            return;
        }

        // 시계 방향(음수)이면 정점이 반대로 깔려 삼각형이 뒷면이 된다.
        // Cull Off 가 아닌 셰이더에서는 통째로 안 보이므로 인덱스를 뒤집어 맞춘다
        BuildTriangles(sweptDegrees < 0f);

        vertices.Clear();
        colors.Clear();
        uvs.Clear();

        // 0번은 중심. 나머지는 호를 따라 늘어선다
        vertices.Add(Vector3.zero);
        colors.Add(Tint(nearColor));
        uvs.Add(ToRect(new Vector2(0.5f, 0.5f)));   // 그림 한가운데

        float step = sweptDegrees / segments;

        for (int i = 0; i <= segments; i++)
        {
            float degrees = startDegrees + step * i;
            float rad = degrees * Mathf.Deg2Rad;

            vertices.Add(new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius);

            // 꼬리(0)에서 스캔 라인(1)으로 가면서 밝아진다
            colors.Add(Tint(Color.Lerp(tailColor, leadColor, (float)i / segments)));

            uvs.Add(ToRect(UvOnDisc(degrees - artAngle)));
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
    }

    /// <summary>
    /// 텍스처를 원판으로 보고 그 위의 좌표를 찍는다.
    /// 중심이 (0.5, 0.5), 테두리가 텍스처 가장자리에 닿는다 —
    /// 그래서 그림을 정사각형 안에 꽉 차는 원으로 그리면 왜곡 없이 잘려 나온다.
    /// </summary>
    static Vector2 UvOnDisc(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 0.5f + new Vector2(0.5f, 0.5f);
    }

    /// <summary>0~1 좌표를 텍스처 안 이 그림의 영역으로 옮긴다.</summary>
    Vector2 ToRect(Vector2 unit)
        => new(uvRect.x + unit.x * uvRect.width,
               uvRect.y + unit.y * uvRect.height);

    Color Tint(Color source)
    {
        source.a *= alpha;
        return source;
    }

    /// <summary>인덱스는 세그먼트 수와 감김 방향이 그대로면 안 바뀐다. 바뀔 때만 다시 만든다.</summary>
    void BuildTriangles(bool reversed)
    {
        if (builtSegments == segments && builtReversed == reversed) return;

        triangles.Clear();

        for (int i = 1; i <= segments; i++)
        {
            triangles.Add(0);

            if (reversed)
            {
                triangles.Add(i + 1);
                triangles.Add(i);
            }
            else
            {
                triangles.Add(i);
                triangles.Add(i + 1);
            }
        }

        builtSegments = segments;
        builtReversed = reversed;
    }
}
