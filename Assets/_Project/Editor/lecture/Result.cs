using UnityEngine;

/// <summary>
/// 게임 결과 UI 스크립트
/// titles[0]: Game Over 타이틀 / titles[1]: Victory 타이틀
/// 게임 종료 시 GameManager에서 Lose() 또는 Win() 호출
/// </summary>
public class Result : MonoBehaviour
{
    [Header("# 결과 타이틀")]
    // [0]: Game Over 이미지 (Title_Dead 스프라이트)
    // [1]: Victory 이미지 (Title_Survive 스프라이트)
    public GameObject[] titles;

    /// <summary>
    /// 플레이어 사망 시 GameManager.GameOverRoutine에서 호출
    /// </summary>
    public void Lose()
    {
        titles[0].SetActive(true); // Game Over 타이틀 표시
    }

    /// <summary>
    /// 제한 시간 생존 시 GameManager.GameVictoryRoutine에서 호출
    /// </summary>
    public void Win()
    {
        titles[1].SetActive(true); // Victory 타이틀 표시
    }
}
