using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{

    public EnemyDataBase enemyDataBase;
    
    int currunthp;
    int damage;
    int light;
    int moveSpeed;
    public virtual void InitEnemy(string name)
    {
        EnemyData data = enemyDataBase.GetEnemyData(name);
        if(data !=null)
        {
            this.currunthp = data.hp;
            this.damage = data.damage;
            this.light = data.light;
            this.moveSpeed = data.moveSpeed;
        }
    }

    public virtual void giveLight()
    {
        //PlayerManger를 만들어서 거기에 재화 관리
        
    }
    
}
