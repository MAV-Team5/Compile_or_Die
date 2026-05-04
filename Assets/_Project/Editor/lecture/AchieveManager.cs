using System;
using System.Collections;
using UnityEngine;

public class AchieveManager : MonoBehaviour
{
    public GameObject[] lockCharacter;
    public GameObject[] unlockCharacter;
    public GameObject uiNotice;

    public enum Achieve { UnlockPotato, UnlockBean }
    Achieve[] achieves;

    WaitForSecondsRealtime wait;

    void Awake()
    {
        achieves = (Achieve[])Enum.GetValues(typeof(Achieve));
        wait     = new WaitForSecondsRealtime(5f);
    }

    void Start()
    {
        Init();
        LockCharacter();
    }

    void LateUpdate()
    {
        foreach (Achieve achieve in achieves)
            CheckAchieve(achieve);
    }

    void Init()
    {
        if (!PlayerPrefs.HasKey("AchieveManager"))
        {
            foreach (Achieve achieve in achieves)
                PlayerPrefs.SetInt(achieve.ToString(), 0);

            PlayerPrefs.SetInt("AchieveManager", 1);
        }
    }

    void LockCharacter()
    {
        for (int i = 0; i < lockCharacter.Length; i++)
        {
            string achieveName = achieves[i].ToString();
            bool isUnlocked    = PlayerPrefs.GetInt(achieveName) == 1;

            lockCharacter[i].SetActive(!isUnlocked);
            unlockCharacter[i].SetActive(isUnlocked);
        }
    }

    void CheckAchieve(Achieve achieve)
    {
        bool isAchieved = false;

        switch (achieve)
        {
            case Achieve.UnlockPotato:
                isAchieved = GameManager.instance.kill >= 10;
                break;
            case Achieve.UnlockBean:
                isAchieved = GameManager.instance.gameTime == GameManager.instance.maxGameTime;
                break;
        }

        if (isAchieved && PlayerPrefs.GetInt(achieve.ToString()) == 0)
        {
            PlayerPrefs.SetInt(achieve.ToString(), 1);
            int index = (int)achieve;
            StartCoroutine(NoticeRoutine(index));
        }
    }

    IEnumerator NoticeRoutine(int index)
    {
        uiNotice.SetActive(true);
        for (int i = 0; i < uiNotice.transform.childCount; i++)
            uiNotice.transform.GetChild(i).gameObject.SetActive(i == index);

        yield return wait;
        uiNotice.SetActive(false);
    }
}
