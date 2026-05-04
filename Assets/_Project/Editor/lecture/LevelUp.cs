using UnityEngine;

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
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
        AudioManager.instance.EffectBgm(true);
    }

    public void Hide()
    {
        rect.localScale = Vector3.zero;
        GameManager.instance.Resume();
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
        AudioManager.instance.EffectBgm(false);
    }

    public void Select(int index)
    {
        items[index].OnClick();
    }

    void Next()
    {
        // 1. 모든 아이템 비활성화
        foreach (Item item in items)
            item.gameObject.SetActive(false);

        // 2. 중복 없는 랜덤 인덱스 3개 생성
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

        // 3. 선택된 3개 활성화 (만렙 시 마지막 소비 아이템으로 대체)
        for (int i = 0; i < randomIndex.Length; i++)
        {
            Item randomItem = items[randomIndex[i]];
            if (randomItem.level == randomItem.data.damages.Length)
                items[items.Length - 1].gameObject.SetActive(true);
            else
                randomItem.gameObject.SetActive(true);
        }
    }
}
