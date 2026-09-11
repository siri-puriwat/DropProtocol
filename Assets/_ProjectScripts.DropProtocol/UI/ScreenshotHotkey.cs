using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DropProtocol
{
/// <summary>F12 writes a supersampled PNG into Recordings/ next to the project or player; the folder is gitignored.</summary>
public sealed class ScreenshotHotkey : MonoBehaviour
{
    private const string ActionPath = "Debug/Screenshot";
    private const string Folder = "Recordings";

    [SerializeField]
    [Range(1, 4)]
    private int m_superSize = 2;

    private InputAction m_action;

    private void Awake()
    {
        var actions = InputSystem.actions;
        m_action = actions != null ? actions.FindAction(ActionPath, false) : null;
    }

    private void OnEnable()
    {
        if (m_action != null)
        {
            m_action.performed += HandleCapture;
            m_action.Enable();
        }
    }

    private void OnDisable()
    {
        if (m_action != null)
        {
            m_action.performed -= HandleCapture;
        }
    }

    private void HandleCapture(InputAction.CallbackContext context)
    {
        Capture();
    }

    public string Capture()
    {
        string directory = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? ".", Folder);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"DropProtocol_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(path, m_superSize);
        return path;
    }
}
}
