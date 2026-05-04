using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터 해금(업적) 시스템 스크립트
/// PlayerPrefs로 해금 상태를 디바이스에 저장/로드
/// LateUpdate에서 매 프레임 조건 체크 → 달성 시 저장 + 알림 표시
/// </summary>
public class AchieveManager : MonoBehaviour
{
    [Header("# 캐릭터 버튼")]
    public GameObject[] lockCharacter;      // 잠금 상태 버튼 [0]=감자농부잠금 [1]=콩농부잠금
    public GameObject[] unlockCharacter;    // 해금 상태 버튼 [0]=감자농부해금 [1]=콩농부해금

    [Header("# 알림 UI")]
    public GameObject uiNotice;             // 해금 알림 UI 오브젝트 (초기 비활성화)

    /// <summary>
    /// 업적 열거형. 이름이 PlayerPrefs 키로 직접 사용됨
    /// Enum.GetValues로 전체 배열 자동 생성 가능
    /// </summary>
    public enum Achieve
    {
        UnlockPotato,   // 감자농부 해금: 언데드 10마리 처치
        UnlockBean      // 콩농부 해금: 생존 성공 (제한 시간 버팀)
    }

    Achieve[] achieves;                     // Achieve 열거형 전체 값 배열

    // WaitForSecondsRealtime: TimeScale=0 에서도 작동하는 대기
    // (레벨업 중 알림이 사라져야 하므로 WaitForSeconds 사용 불가)
    WaitForSecondsRealtime wait;

    void Awake()
    {
        // 열거형 전체 값을 배열로 변환 (업적 추가 시 자동 반영)
        // Enum.GetValues 반환값은 Array → (Achieve[])로 명시적 형변환 필요
        achieves = (Achieve[])Enum.GetValues(typeof(Achieve));
        wait     = new WaitForSecondsRealtime(5f); // 알림 표시 시간: 5초
    }

    void Start()
    {
        Init();           // 최초 실행 시 PlayerPrefs 초기화
        LockCharacter();  // 저장된 해금 상태 읽어서 버튼 표시/숨김
    }

    void LateUpdate()
    {
        // 매 프레임 모든 업적 조건 체크 (Update 후 실행)
        foreach (Achieve achieve in achieves)
            CheckAchieve(achieve);
    }

    /// <summary>
    /// 게임 최초 실행 시에만 모든 업적을 0(미달성)으로 초기화
    /// "AchieveManager" 키가 없으면 = 처음 실행
    /// </summary>
    void Init()
    {
        if (!PlayerPrefs.HasKey("AchieveManager"))
        {
            foreach (Achieve achieve in achieves)
                // ToString(): 열거형 이름을 문자열로 변환 → 키로 사용
                // 예: Achieve.UnlockPotato → "UnlockPotato"
                PlayerPrefs.SetInt(achieve.ToString(), 0); // 0 = 미달성

            PlayerPrefs.SetInt("AchieveManager", 1); // 초기화 완료 표시
        }
    }

    /// <summary>
    /// PlayerPrefs에서 해금 상태를 읽어 잠금/해금 버튼 표시 결정
    /// </summary>
    void LockCharacter()
    {
        for (int i = 0; i < lockCharacter.Length; i++)
        {
            string key        = achieves[i].ToString();
            bool   isUnlocked = PlayerPrefs.GetInt(key) == 1; // 1=해금, 0=잠금

            lockCharacter[i].SetActive(!isUnlocked);   // 잠금 버튼: 해금됐으면 숨김
            unlockCharacter[i].SetActive(isUnlocked);  // 해금 버튼: 해금됐으면 표시
        }
    }

    /// <summary>
    /// 개별 업적 조건 체크. 달성 + 아직 저장 안 됐을 때만 처리
    /// </summary>
    void CheckAchieve(Achieve achieve)
    {
        bool isAchieved = false;

        switch (achieve)
        {
            case Achieve.UnlockPotato:
                // 감자농부: 언데드 10마리 이상 처치
                isAchieved = GameManager.instance.kill >= 10;
                break;

            case Achieve.UnlockBean:
                // 콩농부: 제한 시간까지 생존 (gameTime이 maxGameTime에 도달)
                isAchieved = GameManager.instance.gameTime == GameManager.instance.maxGameTime;
                break;
        }

        // 달성됐고 && 아직 저장 안 된 경우만 처리 (중복 실행 방지)
        if (isAchieved && PlayerPrefs.GetInt(achieve.ToString()) == 0)
        {
            PlayerPrefs.SetInt(achieve.ToString(), 1); // 달성 저장

            // 해당 인덱스의 알림 UI 표시
            int index = (int)achieve; // 열거형 → int 변환 (배열 인덱스로 사용)
            StartCoroutine(NoticeRoutine(index));
        }
    }

    /// <summary>
    /// 해금 알림 표시 코루틴. 5초 후 자동으로 숨김
    /// WaitForSecondsRealtime: TimeScale=0 에서도 타이머 작동
    /// </summary>
    IEnumerator NoticeRoutine(int index)
    {
        uiNotice.SetActive(true);

        // uiNotice의 자식 오브젝트 중 index번째만 활성화
        for (int i = 0; i < uiNotice.transform.childCount; i++)
            uiNotice.transform.GetChild(i).gameObject.SetActive(i == index);

        yield return wait; // 5초 대기 (TimeScale 무관)
        uiNotice.SetActive(false);
    }
}
