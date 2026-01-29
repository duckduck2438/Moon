using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyBehavior
{

    void Move();
    void Attack();
    void TakeDamage();
    void Die();
    
}
