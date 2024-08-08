using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

[InitializeOnLoad]
public class CommandListener
{
    private static TcpListener _listener;
    private static Thread _listenerThread;
    private static object _queueLock;
    private static Queue<Action> _pendingActions;

    private const int Port = 50000;

    private static bool _isInitialized;

    private static bool IsEnabled
    {
        get => PlayerPrefs.GetInt("CommandListenerEnabled", 0) == 1;
        set => PlayerPrefs.SetInt("CommandListenerEnabled", value ? 1 : 0);
    }

    static CommandListener()
    {
        if (_isInitialized)
        {
            CleanUp();
        }

        if (!IsEnabled)
            return;

        Start();
    }

    [MenuItem("Tools/Command Listener/Enable")]
    private static void Enable()
    {
        if (IsEnabled)
            return;

        IsEnabled = true;

        Start();

        Debug.Log("Command Listener enabled");
    }

    [MenuItem("Tools/Command Listener/Disable")]
    private static void Disable()
    {
        if (!IsEnabled)
            return;

        IsEnabled = false;
        CleanUp();

        Debug.Log("Command Listener disabled");
    }

    private static void Start()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        _queueLock = new object();
        _pendingActions = new Queue<Action>();

        EditorApplication.update += OnEditorUpdate;
        StartListener();
    }

    private static void CleanUp()
    {
        if (!_isInitialized)
            return;

        StopListener();
        //set everything to null
        _queueLock = null;
        _pendingActions = null;
        _isInitialized = false;
        EditorApplication.update -= OnEditorUpdate;
    }

    private static void StartListener()
    {
        StopListener();

        _listener = new TcpListener(System.Net.IPAddress.Loopback, Port);
        _listener.Start();

        _listenerThread = new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    if (_listener == null)
                    {
                        Debug.LogError("Listener is null");
                        return;
                    }

                    var client = _listener.AcceptTcpClient();
                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        try
                        {
                            var bufferInfo = await reader.ReadLineAsync();
                            var buffer = bufferInfo.Trim().Split(" ");
                            if (buffer.Length > 0)
                            {
                                var command = buffer[0];
                                var args = buffer[1..];
                                var className = command.Split(".")[0];
                                var methodName = command.Split(".")[1];

                                lock (_queueLock)
                                {
                                    _pendingActions.Enqueue(() => InvokeMethod(className, methodName, args));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        })
        {
            IsBackground = true
        };
        _listenerThread.Start();
    }

    private static void OnEditorUpdate()
    {
        lock (_queueLock)
        {
            while (_pendingActions.Count > 0)
            {
                var action = _pendingActions.Dequeue();
                action.Invoke();
            }
        }
    }

    private static void InvokeMethod(string className, string methodName, string[] args)
    {
        try
        {
            var type = Type.GetType(className);
            if (type != null)
            {
                var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    object[] convertedArgs = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        convertedArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                    }

                    method.Invoke(null, convertedArgs);
                }
                else
                {
                    Debug.LogError("Method not found: " + methodName);
                }
            }
            else
            {
                Debug.LogError("Class not found: " + className);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private static void StopListener()
    {
        _listener?.Stop();
        _listener = null;

        if (_listenerThread != null && _listenerThread.IsAlive)
        {
            _listenerThread.Abort();
            _listenerThread = null;
        }
    }
}
