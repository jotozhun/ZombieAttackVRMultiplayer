using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty triggerValue;
    public InputActionProperty gripValue;

    [Header("Hand Animator")]
    public Animator handAnimator;

    private float triggerFloat;
    private float gripFloat;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        triggerFloat = triggerValue.action.ReadValue<float>();
        gripFloat = gripValue.action.ReadValue<float>();

        handAnimator.SetFloat("Trigger", triggerFloat);
        handAnimator.SetFloat("Grip", gripFloat);
    }
}
