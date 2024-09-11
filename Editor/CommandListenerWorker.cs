using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

public class CommandListenerWorker : IDisposable
{
    private TcpListener _listener;
    private Thread _listenerThread;
    private Queue<Action> _pendingActions;
    private volatile bool _isRunning;

    public CommandListenerWorker()
    {
        _pendingActions = new Queue<Action>();
        _listener = new TcpListener(System.Net.IPAddress.Loopback, CommandListener.Port);
        _listener.Start();

        _isRunning = true;
        StartThread();

        EditorApplication.update += OnEditorUpdate;
    }

    private void StartThread()
    {
        _listenerThread = new Thread(async () =>
        {
            while (_isRunning)
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
                                _pendingActions.Enqueue(() => InvokeMethod(className, methodName, args));
                                Debug.Log($"Command received: {command}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        })
        {
            IsBackground = true
        };
        _listenerThread.Start();
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

    private void OnEditorUpdate()
    {
        while (_pendingActions.Count > 0)
        {
            var action = _pendingActions.Dequeue();
            action.Invoke();
        }
    }

    public void Dispose()
    {
        _isRunning = false; // Set the flag to false to stop the thread

        _listener?.Stop();
        _listener = null;

        if (_listenerThread != null && _listenerThread.IsAlive)
        {
            _listenerThread.Join(); // Wait for the thread to exit gracefully
            _listenerThread = null;
        }

        EditorApplication.update -= OnEditorUpdate;
    }

    ~CommandListenerWorker()
    {
        Dispose();
    }
}
