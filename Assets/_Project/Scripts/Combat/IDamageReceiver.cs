public interface IDamageReceiver
{
    void TakeDamage(float amount);

    /// <summary>
    /// 지금 피해를 받을 수 있는가. 데드락 잠금처럼 <b>한시적으로 무적</b>인 상태를 위한 것.
    ///
    /// <b>TakeDamage 안에서 막지 않고 여기서 미리 거르는 이유</b> — 안에서 막으면
    /// 파이프라인이 이미 표식을 소비하고 피해 숫자까지 띄운 뒤다. 무적인데 숫자가 뜨면
    /// 플레이어는 "안 죽네" 가 아니라 "이 게임 깨졌네" 로 읽는다.
    /// </summary>
    bool AcceptsDamage { get; }
}
