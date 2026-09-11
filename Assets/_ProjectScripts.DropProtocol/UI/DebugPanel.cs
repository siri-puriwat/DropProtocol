using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>
///     Host-only developer controls behind F1: debug damage, director and bot toggles. Only the host is the
///     server, so only it may touch health or spawn; clients never see the panel.
/// </summary>
public sealed class DebugPanel : HudView
{
    private const float DebugReviveFraction = 0.5f;
    private const string ToggleActionPath = "Debug/ToggleDebugPanel";

    [SerializeField]
    private GameObject m_root;

    [SerializeField]
    private Button m_damageButton;

    [SerializeField]
    private Button m_downButton;

    [SerializeField]
    private Button m_reviveButton;

    [SerializeField]
    private Button m_directorButton;

    [SerializeField]
    private TMP_Text m_directorLabel;

    [SerializeField]
    private Button[] m_spawnButtons = System.Array.Empty<Button>();

    [SerializeField]
    private Button m_botsButton;

    [SerializeField]
    private TMP_Text m_botsLabel;

    [SerializeField]
    private TMP_Text m_readout;

    private InputAction m_toggle;
    private bool m_wanted;

    public bool IsShown => m_root != null && m_root.activeSelf;

    private void Awake()
    {
        Listen(m_damageButton, () => Damage(25));
        Listen(m_downButton, () => Damage(int.MaxValue));
        Listen(m_reviveButton, Revive);
        Listen(m_directorButton, ToggleDirector);
        Listen(m_botsButton, ToggleBots);
        for (int i = 0; i < m_spawnButtons.Length; i++)
        {
            int index = i;
            Listen(m_spawnButtons[i], () => Spawn(index));
        }

        var actions = InputSystem.actions;
        m_toggle = actions != null ? actions.FindAction(ToggleActionPath, false) : null;
    }

    private void OnEnable()
    {
        if (m_toggle != null)
        {
            m_toggle.performed += HandleToggle;
            m_toggle.Enable();
        }

        Apply();
    }

    protected override void OnDisable()
    {
        if (m_toggle != null)
        {
            m_toggle.performed -= HandleToggle;
        }

        base.OnDisable();
    }

    private void Update()
    {
        Apply();
    }

    public void Toggle()
    {
        m_wanted = !m_wanted;
        Apply();
    }

    private void HandleToggle(InputAction.CallbackContext context)
    {
        Toggle();
    }

    private void Apply()
    {
        var session = NetworkSession.Instance;
        bool show = m_wanted && session != null && session.IsHost;
        if (m_root != null && m_root.activeSelf != show)
        {
            m_root.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        var director = Object.FindFirstObjectByType<EnemyDirector>();
        var spawner = Object.FindFirstObjectByType<PlayerSpawner>();
        if (m_directorLabel != null)
        {
            m_directorLabel.text = director != null && director.Running ? "Stop director" : "Start director";
        }

        if (m_botsLabel != null)
        {
            m_botsLabel.text = spawner != null && spawner.FillWithBots ? "Remove bots" : "Fill bots";
        }

        if (m_readout != null)
        {
            string budget = director != null ? $"  Budget {director.Budget:0.0}" : string.Empty;
            string bots = spawner != null ? $"  Bots {spawner.BotCount}" : string.Empty;
            m_readout.text = $"Enemies {EnemyCharacter.All.Count}{budget}{bots}";
        }
    }

    private void Damage(int amount)
    {
        var health = Player != null ? Player.Health : null;
        if (health != null)
        {
            health.ApplyDamage(amount == int.MaxValue ? health.Current.Value : amount);
        }
    }

    private void Revive()
    {
        var health = Player != null ? Player.Health : null;
        if (health != null)
        {
            health.Revive(HealthRules.ReviveHitPoints(health.Max, DebugReviveFraction));
        }
    }

    private static void ToggleDirector()
    {
        var director = Object.FindFirstObjectByType<EnemyDirector>();
        if (director != null)
        {
            director.Running = !director.Running;
        }
    }

    private static void Spawn(int index)
    {
        var director = Object.FindFirstObjectByType<EnemyDirector>();
        if (director != null && index < director.Prefabs.Count)
        {
            director.Spawn(index);
        }
    }

    private static void ToggleBots()
    {
        var spawner = Object.FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.FillWithBots = !spawner.FillWithBots;
        }
    }

    private static void Listen(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }
}
}
