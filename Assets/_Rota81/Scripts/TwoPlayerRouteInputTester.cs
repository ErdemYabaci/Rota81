using UnityEngine;
using UnityEngine.InputSystem;

public class TwoPlayerRouteInputTester : MonoBehaviour
{
    [Header("Players")]
    public PlayerRouteController player1;
    public PlayerRouteController player2;

    private InputAction player1NextAction;
    private InputAction player2NextAction;
    private InputAction resetAction;

    private void Awake()
    {
        player1NextAction = new InputAction(
            name: "Player1NextCity",
            type: InputActionType.Button,
            binding: "<Keyboard>/q"
        );

        player2NextAction = new InputAction(
            name: "Player2NextCity",
            type: InputActionType.Button,
            binding: "<Keyboard>/p"
        );

        resetAction = new InputAction(
            name: "ResetBothPlayers",
            type: InputActionType.Button,
            binding: "<Keyboard>/r"
        );

        player1NextAction.performed += _ => MovePlayer1();
        player2NextAction.performed += _ => MovePlayer2();
        resetAction.performed += _ => ResetPlayers();
    }

    private void OnEnable()
    {
        player1NextAction.Enable();
        player2NextAction.Enable();
        resetAction.Enable();
    }

    private void OnDisable()
    {
        player1NextAction.Disable();
        player2NextAction.Disable();
        resetAction.Disable();
    }

    private void OnDestroy()
    {
        player1NextAction.Dispose();
        player2NextAction.Dispose();
        resetAction.Dispose();
    }

    private void MovePlayer1()
    {
        if (player1 == null)
            return;

        player1.MoveToNextCity();
    }

    private void MovePlayer2()
    {
        if (player2 == null)
            return;

        player2.MoveToNextCity();
    }

    private void ResetPlayers()
    {
        if (player1 != null)
            player1.ResetRoute();

        if (player2 != null)
            player2.ResetRoute();

        Debug.Log("İki oyuncunun rotası sıfırlandı.");
    }
}