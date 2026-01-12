using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyVisible : MonoBehaviour
{

    Camera mainCam;

    [Header("Option")]
    public float margin;

    void Start()
    {
        mainCam = Camera.main;
        InvokeRepeating(nameof(CheckVisible), 0f, 0.3f);
    }


    void CheckVisible()
    {
            Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);

    bool isVisible =
        viewportPos.x > -margin && viewportPos.x < 1 + margin &&
        viewportPos.y > -margin && viewportPos.y < 1 + margin &&
        viewportPos.z > 0;

    gameObject.SetActive(isVisible);
    }


}
