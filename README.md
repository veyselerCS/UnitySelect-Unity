# Unity Command Listener

This repository contains a Unity project that includes a custom `CommandListener` script, which listens for TCP commands and executes corresponding methods within the Unity Editor. It also includes a utility for selecting assets in the Unity project.

## Installation

1. **Clone the repository**:
    ```sh
    git clone [<repository-url>](https://github.com/veyselerCS/UnitySelect-Unity)
    ```

2. **Copy the Editor folder**:
    - Copy the `Editor` folder to your Unity project's `Assets` directory.

3. **Enable the Command Listener**:
    - In the Unity Editor, go to `Tools` > `Command Listener` > `Enable`.

## Usage

### Command Listener

1. **Create a static class with a static method**:
    ```csharp
    public static class MyClass
    {
        public static void MyMethod(int number, string message)
        {
            Debug.Log($"Received number: {number}, message: {message}");
        }
    }
    ```

2. **Send a command to invoke the method**:
    - Use a TCP client to send the command in the format `ClassName.MethodName arg1 arg2 ...`.
    - Example command: `MyClass.MyMethod 123 "Hello Unity"`

3. **Check the Unity console for the output**:
    ```
    Received number: 123, message: Hello Unity
    ```

### Select Asset Utility

The package includes a utility script for selecting assets in the Unity project. To use this utility, call the `SelectAssetInProject` method with the path to the asset you want to select:

```csharp
SelectAsset.SelectAssetInProject("Assets/Path/To/Your/Asset");
