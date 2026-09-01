using System.Collections.Generic;
using UnityEngine;

/// <summary>보유 증강을 HUD 슬롯으로 표시한다.</summary>
public class AugmentHud : MonoBehaviour
{
    [SerializeField] AugmentManager manager;
    [SerializeField] AugmentHudSlot slotPrefab;

    readonly List<AugmentHudSlot> slots = new();

    /// <summary>
    /// 다른 화면 위로 올린다. 증강을 고르는 동안에도 지금 무엇을 갖고 있는지
    /// 훑어볼 수 있어야 하는데, 선택 오버레이가 화면을 덮으면 마우스가 안 닿는다.
    /// </summary>
    public void BringToFront() => transform.SetAsLastSibling();

    void Update()
    {
        if (manager == null) return;

        SyncSlots();

        for (int i = 0; i < slots.Count; i++)
            slots[i].Refresh();
    }

    void SyncSlots()
    {
        IReadOnlyList<AugmentRunner> runners = manager.Runners;

        for (int i = slots.Count; i < runners.Count; i++)
        {
            AugmentHudSlot slot = Instantiate(slotPrefab, transform);
            slot.Bind(runners[i]);
            slots.Add(slot);
        }
    }
}