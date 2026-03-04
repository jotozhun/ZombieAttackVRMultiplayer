using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class InputTest : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty testActionValue;
    public InputActionProperty testActionButton;


    void Start()
    {
    }

    void Update()
    {
        float value = testActionValue.action.ReadValue<float>();

        bool button = testActionValue.action.IsPressed();
    }
}
