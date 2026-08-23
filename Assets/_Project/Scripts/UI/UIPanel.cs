using UnityEngine;

// 개별 UI 열고 닫기 컴포넌트.
public class UIPanel : MonoBehaviour
{
    public void Open()
    {
        gameObject.SetActive(true);
        //to do 효과음
    }

    public void Close()
    {
        gameObject.SetActive(false);
        //to do 효과음
    }
}
