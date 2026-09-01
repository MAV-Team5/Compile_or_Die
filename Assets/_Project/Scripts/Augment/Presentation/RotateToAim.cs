using UnityEngine;

/// <summary>방향을 받으면 그쪽으로 회전하는 연출. 원형·대칭이 아닌 부채꼴·검기용.</summary>
public class RotateToAim : MonoBehaviour, IDirectionalVisual
{
    public void Aim(Vector2 direction) => transform.up = direction;
}