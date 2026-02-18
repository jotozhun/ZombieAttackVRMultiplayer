using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.XR;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform vrTransform;
    [SerializeField] private float moveSpeed = 5f;
    [Header("Input")]
    [Tooltip("Assign an Input Action (Vector2) mapped to the left/thumb stick. Works with the new Input System.")]
    public InputActionProperty moveAction;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        Vector2 input = Vector2.zero;

        if (moveAction != null && moveAction.action != null && moveAction.action.enabled)
        {
            input = moveAction.action.ReadValue<Vector2>();
        }
        else if (Gamepad.current != null)
        {
            input = Gamepad.current.leftStick.ReadValue();
        }
        else if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
        }

        if (input.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            Vector3 worldMove = vrTransform.TransformDirection(dir) * moveSpeed * Time.fixedDeltaTime;
            vrTransform.position += worldMove;
        }
    }
}
