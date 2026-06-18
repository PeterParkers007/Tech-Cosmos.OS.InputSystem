using System;
using System.Collections.Generic;
using UnityEngine;
using TechCosmos.InputSystem.Runtime.Structs;
namespace TechCosmos.InputSystem.Runtime
{
    public class InputSystem
    {
        // 无参动作委托
        public delegate void InputAction();
        // 带1个参数的动作委托（可扩展多个参数）
        public delegate void InputAction<T>(T param);

        private Dictionary<string, (InputAction, KeyCode, InputType)> _noParamActions = new();

        private Dictionary<string, (Delegate Action, KeyCode KeyCode, InputType InputType, Func<object> ParamGenerator)> _paramActions = new();


        public bool IsCommandRegistered(string actionName) => _noParamActions.ContainsKey(actionName);

        public bool DetectKeyDownInput(KeyCode keyCode) => Input.GetKeyDown(keyCode);

        public void RegisterCommand(string commandName, KeyCode keyCode, InputAction action, InputType inputType = InputType.KeyDown)
        {
            if (_noParamActions.ContainsKey(commandName))
            {
                Debug.LogWarning($"无参动作 '{commandName}' 已存在，将被覆盖");
            }
            _noParamActions[commandName] = (action, keyCode, inputType);
        }

        // 带参动作注册（泛型方法，T是参数类型）
        public void RegisterCommand<T>(string commandName, KeyCode keyCode, InputAction<T> action, Func<T> paramGenerator, InputType inputType = InputType.KeyDown)
        {
            if (_paramActions.ContainsKey(commandName))
            {
                Debug.LogWarning($"带参动作 '{commandName}' 已存在，将被覆盖");
            }

            Delegate nonGenericAction = action;
            Func<object> nonGenericParamGenerator = () => paramGenerator.Invoke(); // 装箱：T→object

            _paramActions[commandName] = (nonGenericAction, keyCode, inputType, nonGenericParamGenerator);
        }
        public void UnRegisterCommand(string commandName)
        {
            _noParamActions.Remove(commandName);
            _paramActions.Remove(commandName);
        }
        public void UnRegisterCommand(params string[] commandNames)
        {
            foreach (var commandName in commandNames)
            {
                UnRegisterCommand(commandName);
            }
        }
        public void UpdateInput()
        {
            // 无参动作：纯触发
            foreach (var (action, keyCode, inputType) in _noParamActions.Values)
            {
                if (CheckInputType(keyCode, inputType))
                {
                    action?.Invoke();
                }
            }

            // 带参动作：纯转发（反射仅用于传递参数，不做任何业务逻辑）
            foreach (var kvp in _paramActions)
            {
                var (action, keyCode, inputType, paramGenerator) = kvp.Value;
                if (CheckInputType(keyCode, inputType))
                {
                    try
                    {
                        // 1. 调用用户的参数生成逻辑（工具不关心返回什么）
                        object param = paramGenerator.Invoke();
                        // 2. 传递参数给用户的动作（反射仅做“调用”，不做类型干预）
                        action?.DynamicInvoke(param);
                    }
                    catch (Exception e)
                    {
                        // 仅抛警告，不处理逻辑（用户自己排查类型不匹配等问题）
                        Debug.LogWarning($"动作 '{kvp.Key}' 执行失败：{e.Message}");
                    }
                }
            }
        }
        // 分离执行逻辑
        public void ExecuteCommand(string actionName)
        {
            if (_noParamActions.TryGetValue(actionName, out var actionData))
            {
                if (CheckInputType(actionData.Item2, actionData.Item3))
                {
                    actionData.Item1?.Invoke();
                }
            }
        }
        public void ExecuteCommand<T>(string actionName, T param)
        {
            if (_paramActions.TryGetValue(actionName, out var actionData))
            {
                if (CheckInputType(actionData.Item2, actionData.Item3) && actionData.Item1 is InputAction<T> action)
                {
                    action?.Invoke(param);
                }
            }
        }
        private bool CheckInputType(KeyCode keyCode, InputType inputType)
        {
            return inputType switch
            {
                InputType.KeyDown => Input.GetKeyDown(keyCode),
                InputType.KeyUp => Input.GetKeyUp(keyCode),
                InputType.KeyHold => Input.GetKey(keyCode),
                _ => false
            };
        }
    }
}

