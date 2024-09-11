using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CommandListener
{
    private static CommandListenerWorker _worker;
    public const int Port = 50000;

    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt("CommandListenerEnabled", 0) == 1;
        set => PlayerPrefs.SetInt("CommandListenerEnabled", value ? 1 : 0);
    }

    static CommandListener()
    {
        EditorApplication.quitting += OnEditorQuitting;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        
        if (!IsEnabled)
            return;
        if (_worker == null)
            _worker = new CommandListenerWorker();
    }

    [MenuItem("Tools/Command Listener/Enable")]
    private static void Enable()
    {
        if (IsEnabled)
            return;

        IsEnabled = true;

        if (_worker != null)
        {
            throw new System.Exception("Command Listener worker already exists");
        }

        new CommandListenerWorker();
        Debug.Log("Command Listener enabled");
    }

    [MenuItem("Tools/Command Listener/Disable")]
    private static void Disable()
    {
        if (!IsEnabled)
            return;

        IsEnabled = false;
        if (_worker == null)
        {
            throw new System.Exception("Command Listener worker does not exist");
        }

        _worker.Dispose();
        _worker = null;
        Debug.Log("Command Listener disabled");
    }

    private static void OnBeforeAssemblyReload()
    {
        // Stop the worker before reloading assemblies
        _worker?.Dispose();
        _worker = null;
    }

    private static void OnEditorQuitting()
    {
        // Clean up the worker on quitting
        _worker?.Dispose();
        _worker = null;
    }
}
