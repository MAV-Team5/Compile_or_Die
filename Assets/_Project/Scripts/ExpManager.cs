using UnityEngine;

/// <summary>
/// 경험치 획득 로그 경험치량 합치기
/// </summary>
public class ExpManager : MonoBehaviour
{
    int pendingExp;

    float timer;

    const float delay = 0.5f;

    public void AddExpLog(int amount)
    {
        pendingExp += amount;
        if (timer <= 0)
        {
            timer = 0.5f;
        }
        else
        {
            timer += 0.15f;
            timer = Mathf.Min(timer, 1.5f);
        }
    }

    void Update()
    {
        if (pendingExp <= 0)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            LogManager.Instance.Exp(
                $"EXP GAINED (+{pendingExp})"
            );

            pendingExp = 0;
        }
    }
}