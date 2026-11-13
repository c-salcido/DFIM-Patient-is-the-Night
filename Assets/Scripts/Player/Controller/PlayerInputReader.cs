using UnityEngine;

// Reads the keyboard/gamepad once per frame so the rest of the controller never calls Input directly.
// It's [Serializable] so the keys show up (and can be rebound) on the PlayerController in the Inspector.
// Uses the old Input Manager because the project doesn't have the new Input System package installed.
[System.Serializable]
public class PlayerInputReader
{
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public KeyCode jumpKey = KeyCode.Z;
    public KeyCode dashKey = KeyCode.C;
    public KeyCode attackKey = KeyCode.X;
    public KeyCode throwKey = KeyCode.V;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool ThrowPressed { get; private set; }

    public void Read()
    {
        Horizontal = Input.GetAxisRaw(horizontalAxis);
        Vertical = Input.GetAxisRaw(verticalAxis);
        JumpPressed = Input.GetKeyDown(jumpKey);
        DashPressed = Input.GetKeyDown(dashKey);
        AttackPressed = Input.GetKeyDown(attackKey);
        ThrowPressed = Input.GetKeyDown(throwKey);
    }

    public void Clear()
    {
        Horizontal = 0f;
        Vertical = 0f;
        JumpPressed = DashPressed = AttackPressed = ThrowPressed = false;
    }
}