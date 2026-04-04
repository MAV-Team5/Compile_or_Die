<<<<<<< Updated upstream
=======
using System.Collections;
using System.Collections.Generic;
>>>>>>> Stashed changes
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
<<<<<<< Updated upstream

    public Player player;
    public PoolManager pool;
    public void Awake()
    {
        instance = this;
    }
=======
    public Player player;
    //public PoolManager pool;

    //public float gameTime;
    //public float maxGameTime = 2 * 10f;

    void Awake()
    {
        instance = this;
    }

    /*void Update()
    {
        gameTime += Time.deltaTime;

        if (gameTime > maxGameTime)
        {
            gameTime = maxGameTime;
        }
    }*/
>>>>>>> Stashed changes
}
