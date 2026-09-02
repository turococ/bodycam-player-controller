using System.Collections;
using UnityEngine;

public class CharacterUIAnimation : MonoBehaviour
{
    HealthSystem healthSystem;
    
    [SerializeField] Animator characterAnimator;


    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        StartCoroutine(BlinkTimer());
    }

    void Update()
    {
        healthStatusUpdate();
    }

    void healthStatusUpdate()
    {
        characterAnimator.SetFloat("health", healthSystem.health);
    }

    IEnumerator BlinkTimer()
    {
        while (true)
        {
            var randomTime = Random.Range(2f, 6f);

            yield return new WaitForSeconds(randomTime);

            characterAnimator.SetTrigger("blink");
        }
    }
}