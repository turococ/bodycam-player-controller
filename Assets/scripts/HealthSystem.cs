using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] Slider healthBar;

    [Range(0,100)]
    [SerializeField] internal int health = 100;

    void Start()
    {

    }

    void Update()
    {
        healtUpdate();
    }

    void healtUpdate()
    {
        healthBar.value = health;
    }
}
