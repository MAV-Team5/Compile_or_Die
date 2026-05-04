using UnityEngine;

/// <summary>
/// 레벨업 창 제어 스크립트
/// </summary>
public class LevelUp : MonoBehaviour
{
    RectTransform rect;
    Item[] items;

    void Awake()
    {
        rect  = GetComponent<RectTransform>();
        items = GetComponentsInChildren<Item>(true);
    }

    public void Show()
    {
        Next();
        rect.localScale = Vector3.one;
        GameManager.instance.Stop();
        PlaySfxSafe(AudioManager.Sfx.LevelUp);
        EffectBgmSafe(true);
    }

    public void Hide()
    {
        rect.localScale = Vector3.zero;
        GameManager.instance.Resume();
        PlaySfxSafe(AudioManager.Sfx.Select);
        EffectBgmSafe(false);
    }

    public void Select(int index)
    {
        items[index].OnClick();
    }

    void Next()
    {
        foreach (Item item in items)
            item.gameObject.SetActive(false);

        int[] randomIndex = new int[3];
        while (true)
        {
            randomIndex[0] = Random.Range(0, items.Length);
            randomIndex[1] = Random.Range(0, items.Length);
            randomIndex[2] = Random.Range(0, items.Length);

            if (randomIndex[0] != randomIndex[1]
             && randomIndex[1] != randomIndex[2]
             && randomIndex[0] != randomIndex[2])
                break;
        }

        for (int i = 0; i < randomIndex.Length; i++)
        {
            Item randomItem = items[randomIndex[i]];

            bool isMaxLevel = randomItem.data != null
                           && randomItem.data.damages != null
                           && randomItem.level >= randomItem.data.damages.Length;

            if (isMaxLevel)
                items[items.Length - 1].gameObject.SetActive(true);
            else
                randomItem.gameObject.SetActive(true);
        }
    }

    // ─── 오디오 null 안전 헬퍼 ──────────────────────────────────────────
    void PlaySfxSafe(AudioManager.Sfx sfx)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(sfx);
    }

    void EffectBgmSafe(bool isPlay)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.EffectBgm(isPlay);
    }
}
