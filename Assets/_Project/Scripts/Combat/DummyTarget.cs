using UnityEngine;

public class DummyTarget : MonoBehaviour, IDamageReceiver
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] bool autoRespawn = true;
    [SerializeField] bool logDamage = true;

    public float CurrentHealth {get; private set;}
    public float MaxHealth => maxHealth;

    public void TakeDamage(float amount)
    {
        if(amount <= 0f)
        {
            return;
        }

        CurrentHealth -= amount;

        // 테스트 씬에는 LogManager가 없을 수 있다
        if (logDamage)
        {
            string msg = $"[{name}] -{amount:0.#}  →  {CurrentHealth:0.#}/{maxHealth:0.#}";

            if (LogManager.Instance != null) LogManager.Instance.AddLog(GameLogType.Combat, msg);
            else Debug.Log(msg, this);
        }

        if(CurrentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (LogManager.Instance != null) LogManager.Instance.AddLog(GameLogType.Combat, $"[{name}] die");
        else Debug.Log($"[{name}] die", this);

        if (autoRespawn) ResetHealth();
        else gameObject.SetActive(false);
    }

    void ResetHealth() => CurrentHealth = maxHealth;

    void OnEnable() => ResetHealth();

    [ContextMenu("피해 10 주기")]
    void TestDamage10() => TakeDamage(10f);

    [ContextMenu("피해 999 주기")]
    void TestDamageLethal() => TakeDamage(999f);

    [ContextMenu("체력 원복")]
    void TestReset() => ResetHealth();

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        float shown = Application.isPlaying ? CurrentHealth : maxHealth;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, $"{shown:0}/{maxHealth:0}");
    }
#endif

}

