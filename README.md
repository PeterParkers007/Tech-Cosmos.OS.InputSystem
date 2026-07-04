# Tech-Cosmos OS Input System

## 概述

Tech-Cosmos OS Input System 是一个专为 Unity 项目设计的、灵活且可扩展的输入管理模块。它允许开发者在编辑器中直观地配置默认按键，并支持在运行时通过 UI 动态地重新绑定按键。系统会自动将用户的自定义按键配置以 JSON 格式保存到本地，并在下次启动时加载。

## 主要特性

*   **可配置的默认按键**：通过 `ScriptableObject` 集中管理所有动作的默认按键绑定。
*   **运行时按键重绑定**：提供简洁的 UI 组件，允许玩家在游戏内自由更改按键。
*   **冲突检测**：在绑定新按键时，自动检测并避免与其他已绑定的动作发生冲突。
*   **本地持久化**：按键配置会自动保存为 JSON 文件，并在游戏启动时加载。
*   **事件驱动**：当按键绑定发生变更时，会触发全局事件，方便其他系统响应。
*   **组合键支持**：支持修饰键组合（如 `Alt + X`、`Ctrl + Shift + C`）以及双键组合（如 `A + 鼠标左键`）。
*   **易于集成**：单例模式的 `InputManager` 可方便地在任何脚本中调用，获取输入状态。

## 文件结构

```
Tech-Cosmos.OS.InputSystem/
└── Runtime/
    ├── InputConfig.cs       # ScriptableObject 配置文件，用于存储所有动作的默认按键列表。
    ├── InputManager.cs      # 核心管理器，处理按键注册、重绑定、持久化及输入查询。
    ├── KeyBindingUI.cs      # UI 组件，用于在游戏中显示并动态重绑定单个按键。
    └── KeyConfig.cs         # 数据结构，含 InputBinding / ModifierKey 与动作名配对。
```

## 快速开始

### 1. 创建配置文件

1.  在 Unity 编辑器的 `Project` 窗口中，右键点击并选择 **Create → Tech-Cosmos → Input → Config**。
2.  将其命名为，例如 `PlayerInputConfig`。
3.  在 `Inspector` 窗口中，设置 `Key Configs` 列表的大小，并添加你的动作（如 "Jump"、"Fire"、"Interact" 等），为每个动作指定默认按键。

**单键绑定（兼容旧配置）：** 只需填写 `Key Code` 字段。

**组合键绑定：** 展开 `Binding` 字段进行配置：

| 字段 | 说明 |
| :--- | :--- |
| `Key` | 主键（触发键），如 `X`、`Mouse0` |
| `Modifiers` | 修饰键标志，可多选 `Control`、`Alt`、`Shift` |
| `Combo Key` | 额外组合键，如 `A`（与主键同时按住时生效） |

**示例 `KeyConfig` 结构：**

| Name       | 绑定方式                          |
| :--------- | :-------------------------------- |
| Jump       | Space                             |
| Fire       | Mouse0                            |
| QuickSave  | Binding: Alt + S                    |
| SpecialAtk | Binding: A + 鼠标左键（Combo Key=A, Key=Mouse0） |
| Pause      | Escape                            |

### 2. 设置 InputManager

1.  在场景中创建一个空的 `GameObject`。
2.  为其添加 `InputManager` 组件。
3.  将第 1 步创建的 `InputConfig` 文件拖拽到 `InputManager` 的 `Config` 字段上。

`InputManager` 会在 `Awake` 中自动将自己设置为单例且跨场景不销毁（`DontDestroyOnLoad`）。

### 3. 创建按键绑定 UI（可选）

1.  在 `Canvas` 下创建一个 `Panel`，用于放置按键绑定入口。
2.  为其添加 `KeyBindingUI` 组件。
3.  在 `Inspector` 中配置该组件：
    *   **Action Name**：填入你要它控制的动作名，例如 "Jump"。
    *   **Key Text**：拖入一个用于显示当前按键的 `Text` 或 `TextMeshProUGUI` 组件。
    *   **Bind Button**：拖入一个用于触发重绑定的 `Button` 组件。
    *   **Ignored Keys**：可选，添加在监听时希望忽略的按键（默认仅忽略 `None`）。鼠标按键现已可用于组合键绑定。
    *   **On Key Rebinded** / **On Listening State Changed**：可选，绑定你自己的 UnityEvent 以响应事件。

当玩家点击 **Bind Button** 时，`KeyBindingUI` 会进入监听状态，等待玩家按下新按键，完成后自动更新 UI 并保存配置。

### 4. 在脚本中查询输入

在任何继承自 `MonoBehaviour` 的脚本中，你都可以通过单例实例来查询输入，无需单独引用。

```csharp
using TechCosmos.InputSystem.Runtime;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void Update()
    {
        // 持续按键（例如：移动）
        if (InputManager.Instance.GetKey("Jump"))
        {
            Debug.Log("跳跃键被按住");
        }

        // 按下瞬间（例如：开火）
        if (InputManager.Instance.GetKeyDown("Fire"))
        {
            Debug.Log("开火键刚按下！");
        }

        // 抬起瞬间
        if (InputManager.Instance.GetKeyUp("Pause"))
        {
            Debug.Log("暂停键被松开，切换暂停状态");
        }
    }
}
```

## 核心 API 参考

### InputManager

| 方法 / 属性 | 说明 |
| :--- | :--- |
| `Instance` | 静态单例访问点。 |
| `Config` | 获取当前关联的 `InputConfig` 资产。 |
| `GetKey(string name)` | 返回指定动作的按键是否**被按住**。 |
| `GetKeyDown(string name)` | 返回指定动作的按键是否在**当前帧按下**。 |
| `GetKeyUp(string name)` | 返回指定动作的按键是否在**当前帧抬起**。 |
| `GetBinding(string name)` | 返回指定动作当前绑定的 `InputBinding`（含修饰键与组合键）。 |
| `GetKeyCode(string name)` | 返回指定动作当前绑定的主键 `KeyCode`。 |
| `RebindKey(string name, InputBinding newBinding)` | 重新绑定指定动作的组合键，默认会进行冲突检测。 |
| `RebindKey(string name, KeyCode newKey)` | 重新绑定为单键（不含修饰键），兼容旧 API。 |
| `IsBindingAlreadyBound(InputBinding binding)` | 检测组合键是否已被其他动作占用。 |
| `ResetToDefault()` | 将所有动作的按键重置为 `InputConfig` 中定义的默认值，并删除存档。 |
| `ResetKeyToDefault(string name)` | 仅将指定动作的按键重置为默认值，并保存。 |
| `SaveBindings()` | 手动保存当前所有按键绑定到本地文件。 |
| `LoadBindings()` | 从本地文件加载并覆盖当前按键绑定。 |
| `GetAllActionNames()` | 返回所有已注册动作名的列表。 |
| `GetAllBindings()` | 返回当前所有动作及其 `InputBinding` 的字典副本。 |
| `GetAllKeyCodes()` | 返回当前所有动作及其主键 `KeyCode` 的字典副本。 |

### KeyBindingUI

| 成员 | 类型 | 说明 |
| :--- | :--- | :--- |
| `actionName` | `string` | 本 UI 组件所控制的动作名。 |
| `keyText` | `Text` | 用于显示当前按键的 UI 文本。 |
| `bindButton` | `Button` | 点击后开始监听新按键的按钮。 |
| `ignoredKeys` | `KeyCode[]` | 监听时需要忽略的按键数组。 |
| `onKeyRebinded` | `UnityEvent<string, InputBinding>` | 按键重绑定成功时触发。 |
| `onListeningStateChanged` | `UnityEvent<bool>` | 监听状态改变时触发。 |
| `StartListening()` | 方法 | 公开方法，可手动调用以开始监听按键。 |
| `CancelListening()` | 方法 | 公开方法，可手动调用以取消监听。 |
| `ResetToDefault()` | 方法 | 公开方法，可手动调用以将此动作按键重置为默认。 |

## 工作流程与架构

1.  **初始化**：`InputManager` 在 `Awake` 时，从 `InputConfig` 读取所有默认按键进行注册。
2.  **加载存档**：之后立即尝试从 `Application.persistentDataPath/keybindings.json` 加载玩家的自定义配置，覆盖默认值。
3.  **运行时查询**：游戏代码通过 `InputManager` 的 `GetKey`、`GetKeyDown` 等方法，以**动作名**（而非具体按键）来查询输入状态，实现了按键与游戏逻辑的解耦。
4.  **重绑定**：通过 `KeyBindingUI` 或直接调用 `InputManager.RebindKey`，更改内存中的映射关系。
5.  **持久化**：重绑定操作会触发 `OnKeyRebinded` 事件，并在成功后将当前所有按键映射序列化并写入 JSON 文件。

### JSON 存档格式示例

```json
{
    "bindings": [
        {
            "name": "Jump",
            "keyCode": 32,
            "modifiers": 0,
            "comboKey": 0
        },
        {
            "name": "QuickSave",
            "keyCode": 115,
            "modifiers": 2,
            "comboKey": 0
        },
        {
            "name": "SpecialAtk",
            "keyCode": 323,
            "modifiers": 0,
            "comboKey": 97
        }
    ]
}
```

（注：`keyCode` / `comboKey` 是 Unity `KeyCode` 枚举的整数值；`modifiers` 是 `ModifierKey` 标志位，如 `2` 代表 `Alt`。旧版仅含 `keyCode` 的存档仍可正常加载。）

## 依赖项

*   **Unity Engine**：`UnityEngine` 核心命名空间。
*   **Unity UI**：`UnityEngine.UI`（用于 `KeyBindingUI` 组件中的 `Text` 和 `Button`）。
*   **System.IO**、**System.Collections**：.NET 标准库，用于文件操作和集合。

## 注意事项

*   请确保场景中仅存在**一个**挂载了 `InputManager` 的 GameObject。如果存在多个，后创建的会自动销毁。
*   `KeyBindingUI` 在监听按键时，按 `Escape` 键会取消本次重绑定操作。
*   在重绑定冲突检测机制下，若新按键已绑定给其他动作，此次重绑定将失败，并输出警告。
*   组合键查询逻辑：`GetKeyDown` 在**最后一个按下的键**触发时返回 true（修饰键需已按住）；`GetKey` 要求所有键同时按住；`GetKeyUp` 在任一组成键松开时返回 true。
*   运行时重绑定时，按住修饰键再按主键即可绑定如 `Alt + X`；按住 `A` 再点鼠标左键即可绑定 `A + 鼠标左键`。
*   若要支持与 Unity 新输入系统（Input System Package）类似的设备级处理，此模块需要额外扩展。