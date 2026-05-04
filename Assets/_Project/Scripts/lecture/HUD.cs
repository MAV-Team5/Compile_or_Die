using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD(게임 내 정보 UI) 표시 스크립트
/// 하나의 스크립트로 여러 UI 요소를 타입별로 관리
/// 각 UI 오브젝트에 부착 후 Inspector에서 InfoType 선택
/// </summary>
public class HUD : MonoBehaviour
{
    /// <summary>
    /// UI 타입 열거형. Inspector에서 드롭다운으로 선택
    /// </summary>
    public enum InfoType
    {
        Exp,    // 경험치 슬라이더 (0~1 비율)
        Level,  // 레벨 텍스트 (Lv.N)
        Kill,   // 킬수 텍스트
        Time,   // 남은 시간 텍스트 (MM:SS)
        Health  // 체력 슬라이더 (0~1 비율)
    }
    public InfoType type; // Inspector에서 이 오브젝트가 어떤 정보를 표시할지 선택

    Text myText;        // 텍스트형 HUD에서 사용 (Level, Kill, Time)
    Slider mySlider;    // 슬라이더형 HUD에서 사용 (Exp, Health)

    void Awake()
    {
        // 이 오브젝트에 Text 또는 Slider 중 하나만 있으면 됨
        myText   = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
    }

    void LateUpdate()
    {
        // Update 완료 후 GameManager 값을 읽어 UI 갱신
        switch (type)
        {
            case InfoType.Exp:
                // 현재 경험치 / 다음 레벨 필요 경험치 = 슬라이더 비율 (0~1)
                float curExp = GameManager.instance.exp;
                float maxExp = GameManager.instance.nextExp[
                    Mathf.Min(GameManager.instance.level, GameManager.instance.nextExp.Length - 1)];
                mySlider.value = curExp / maxExp;
                break;

            case InfoType.Level:
                myText.text = string.Format("Lv.{0:F0}", GameManager.instance.level);
                break;

            case InfoType.Kill:
                myText.text = string.Format("{0:F0}", GameManager.instance.kill);
                break;

            case InfoType.Time:
                // 남은 시간 계산 후 MM:SS 형식으로 표시
                float remain = GameManager.instance.maxGameTime - GameManager.instance.gameTime;
                int min = Mathf.FloorToInt(remain / 60); // 몫 = 분
                int sec = Mathf.FloorToInt(remain % 60); // 나머지 = 초
                // D2: 최소 2자리, 부족하면 앞에 0 채움 (예: 5 → "05")
                myText.text = string.Format("{0:D2}:{1:D2}", min, sec);
                break;

            case InfoType.Health:
                mySlider.value = GameManager.instance.health / GameManager.instance.maxHealth;
                break;
        }
    }
}
