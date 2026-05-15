using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ZombieAttackVR.Weapons;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Prefabs")]
    [SerializeField] private NetworkPrefabRef _vrPlayerPrefab;

    [Header("Input - Action References del XRI Default Input Actions")]
    [Tooltip("Activate / trigger del XR (semi/auto). El cliente lo recolecta en OnInput.")]
    [SerializeField] private InputActionReference _triggerLeftAction;
    [SerializeField] private InputActionReference _triggerRightAction;
    [Tooltip("Secondary button (Y/B) — para recarga.")]
    [SerializeField] private InputActionReference _reloadLeftAction;
    [SerializeField] private InputActionReference _reloadRightAction;

    [Header("Weapon Position Source")]
    [Tooltip("Transform del muzzle/hand actualmente sosteniendo el arma (mano derecha). " +
             "Se asigna en runtime cuando el jugador agarra un arma. Si está null, " +
             "OnInput enviará Vector3.zero y el host caerá al transform local del arma.")]
    [SerializeField] private Transform _activeWeaponMuzzle;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    /// <summary>
    /// Punto de inyección externo: el componente que gestiona el grab de arma
    /// puede actualizar este transform para que OnInput reporte la posición correcta.
    /// </summary>
    public Transform ActiveWeaponMuzzle
    {
        get => _activeWeaponMuzzle;
        set => _activeWeaponMuzzle = value;
    }

    async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "L4DVR_Room_01",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return; // Solo el host instancia jugadores.
        if (_vrPlayerPrefab == default) return;

        // Spawn del prefab del jugador, asignando input authority al PlayerRef.
        var playerObj = runner.Spawn(
            _vrPlayerPrefab,
            position: Vector3.zero,
            rotation: Quaternion.identity,
            inputAuthority: player);

        _spawnedPlayers[player] = playerObj;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out var obj) && obj != null)
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var weaponInput = new WeaponInput();

        // Lee el estado de las acciones XR locales. Se incluyen ambas manos
        // porque cualquiera puede estar sosteniendo el arma activa.
        bool triggerPressed = ReadButton(_triggerLeftAction) || ReadButton(_triggerRightAction);
        bool reloadPressed = ReadButton(_reloadLeftAction) || ReadButton(_reloadRightAction);

        weaponInput.buttons.Set(WeaponButton.Trigger, triggerPressed);
        weaponInput.buttons.Set(WeaponButton.Reload, reloadPressed);

        if (_activeWeaponMuzzle != null)
        {
            weaponInput.weaponPosition = _activeWeaponMuzzle.position;
            weaponInput.weaponForward = _activeWeaponMuzzle.forward;
        }

        input.Set(weaponInput);
    }

    private static bool ReadButton(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return false;
        if (!actionRef.action.enabled) actionRef.action.Enable();
        // Para acciones tipo Button, IsPressed() devuelve true si el botón sigue presionado.
        return actionRef.action.IsPressed();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
