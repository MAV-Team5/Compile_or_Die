using UnityEngine;

public class Result : MonoBehaviour
{
    public GameObject[] titles;   // [0]: Game Over, [1]: Victory

    public void Lose()
    {
        titles[0].SetActive(true);
    }

    public void Win()
    {
        titles[1].SetActive(true);
    }
}
