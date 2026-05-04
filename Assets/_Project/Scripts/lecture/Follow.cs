using UnityEngine;

/// <summary>
/// 체력바 UI가 플레이어를 따라다니게 하는 스크립트
/// 월드 좌표(플레이어 위치) → 스크린 좌표(UI 위치)로 변환
/// Health 오브젝트(빈 오브젝트)에 부착
/// </summary>
public class Follow : MonoBehaviour
{
    RectTransform rect; // UI 오브젝트의 위치 제어용 (Transform 대신 RectTransform 사용)

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void FixedUpdate()
    {
        // 플레이어가 FixedUpdate에서 이동하므로 Follow도 FixedUpdate 사용
        // → 타이밍 일치로 UI 떨림 방지
        // WorldToScreenPoint: 게임 월드 좌표 → 스크린 픽셀 좌표 변환
        rect.position = Camera.main.WorldToScreenPoint(
            GameManager.instance.player.transform.position);
    }
}
