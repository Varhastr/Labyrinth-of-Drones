using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnScript : MonoBehaviour
{
    public GameObject theEnemy;
    private int xPos;
    private int zPos;
    public int maxEnemySpawn;
    public int enemyCount;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        EnemyDrop();
    }
    void EnemyDrop() 
    {
        while (enemyCount < maxEnemySpawn) 
        {
            xPos = Random.Range(12, -414);
            zPos = Random.Range(13, 441);
            Instantiate(theEnemy, new Vector3(xPos, 1, zPos), Quaternion.identity);
            enemyCount += 1;
        }
    }
}
