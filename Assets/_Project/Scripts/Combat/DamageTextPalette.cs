using UnityEngine;

/// <summary>
/// 피해 숫자 색·크기를 한곳에 모아둔 표. 기획자가 코드 없이 조정한다.
///
/// 고르는 순서 — 크리티컬 → 효과가 직접 지정 → 증강 분류 → 기본
/// 분류만 채워두면 증강마다 색을 정하지 않아도 자동으로 구분된다.
/// </summary>
[CreateAssetMenu(fileName = "DamageTextPalette", menuName = "CoD/Damage Text Palette")]
public class DamageTextPalette : ScriptableObject
{
    [System.Serializable]
    public struct CategoryStyle
    {
        public AugmentCategory category;
        public DamageTextStyle style;
    }

    [Tooltip("아무 조건도 안 걸릴 때. 옛 무기·환경 피해 등.")]
    public DamageTextStyle normal = DamageTextStyle.Default;

    [Tooltip("치명타. 다른 무엇보다 우선한다.")]
    public DamageTextStyle critical = new()
    {
        color = new Color(1f, 0.85f, 0.3f),
        scale = 1.5f,
        riseSpeed = 2.2f
    };

    [Tooltip("증강 분류별 색. 비워둔 분류는 기본 스타일을 쓴다.")]
    public CategoryStyle[] byCategory = System.Array.Empty<CategoryStyle>();

    /// <summary>이 피해에 쓸 스타일을 고른다.</summary>
    public DamageTextStyle Resolve(DamageContext dmg)
    {
        if (dmg.IsCritical) return critical.Filled();

        // 효과가 직접 지정했으면 분류보다 우선한다 — 특정 증강만 튀게 하고 싶을 때
        if (dmg.StyleOverride.HasValue) return dmg.StyleOverride.Value.Filled();

        if (dmg.SourceAugment != null && dmg.SourceAugment.Data != null)
        {
            AugmentData data = dmg.SourceAugment.Data;
            DamageTextStyle style = normal.Filled();

            for (int i = 0; i < byCategory.Length; i++)
                if (byCategory[i].category == data.category) { style = byCategory[i].style.Filled(); break; }

            // 숫자 색은 그 증강의 고유색 — 아이콘·투사체 꼬리·연쇄선과 같은 색이라 누가 때렸는지 보인다
            if (AugmentPalette.TryGet(data, out Color own)) style.color = own;

            return style;
        }

        return normal.Filled();
    }
}
