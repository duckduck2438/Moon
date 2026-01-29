using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataBase", menuName = "Enemy/Enemy Data Base")]
public class EnemyDataBase : ScriptableObject
{

    public List<EnemyData> enemyDatas = new List<EnemyData>();

    public EnemyData GetEnemyData(string name)
    {
        foreach (var data in enemyDatas)
        {
            if (data.name == name)
            {
                return data;
            }
        }
        return null;
    }
}
