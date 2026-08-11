/// <summary>넉백·견인처럼 외부에서 위치를 밀 수 있는 대상.</summary>
public interface IDisplaceable
{
    /// <summary>즉시 delta 만큼 옮기고, suppressDuration 동안 스스로 움직이지 않는다.</summary>
    void Displace(UnityEngine.Vector2 delta, float suppressDuration);
}
