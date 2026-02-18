using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    public InputActionProperty testActionValue;
    public InputActionProperty testActionButton;

    void Start()
    {
        
    }

    void Update()
    {
        float value = testActionValue.action.ReadValue<float>();
        Debug.Log("VALUE : " + value);

        bool button = testActionValue.action.IsPressed();
        Debug.Log("BUTTON : " + button);
    }
}
