# Tech-Cosmos Input System

一个轻量级、可扩展的 Unity 按键绑定系统，支持静态预设按键和运行时动态增删按键，附带完整的改键 UI 组件。

---

## 目录

- [特性](#特性)
- [文件结构](#文件结构)
- [快速开始](#快速开始)
- [核心概念](#核心概念)
- [API 参考](#api-参考)
- [使用示例](#使用示例)
- [高级用法](#高级用法)
- [常见问题](#常见问题)

---

## 特性

- **静态按键**：通过 ScriptableObject 预设，Inspector 可视化配置
- **动态按键**：运行时通过 API 增删，不修改 ScriptableObject
- **统一 API**：静态和动态按键使用完全相同的接口检测输入
- **改键系统**：支持运行时重新绑定，自动检测冲突
- **组合键**：支持 Ctrl/Alt/Shift 修饰键和双键组合
- **鼠标支持**：支持鼠标左/中/右键
- **持久化**：自动保存/加载按键配置到 JSON 文件
- **UI 组件**：开箱即用的 KeyBindingUI，支持绑定/取消/重置
- **事件驱动**：按键增删改均有事件通知，方便 UI 自动更新

---

## 文件结构

```
Tech-Cosmos.OS.InputSystem/
└── Runtime/
    ├── InputConfig.cs      # ScriptableObject 配置文件
    ├── KeyConfig.cs        # InputBinding 结构体、ModifierKey 枚举、KeyConfig 结构体
    ├── InputManager.cs     # 核心管理器（单例）
    └── KeyBindingUI.cs     # 改键 UI 组件
```

---

## 快速开始

### 1. 创建配置文件

在 Unity 中右键 → `Create → Tech-Cosmos → Input → Config`，创建 `InputConfig`。

在 Inspector 中配置预设按键：

```
Key Configs:
  - Name: "Jump"     Binding: Space
  - Name: "Fire"     Binding: Mouse0
  - Name: "Reload"   Binding: R
```

### 2. 挂载 InputManager

创建一个空 GameObject，命名为 `InputManager`，挂载 `InputManager` 组件，将刚才创建的 `InputConfig` 拖入 `Config` 字段。

### 3. 检测输入

```csharp
using TechCosmos.InputSystem.Runtime;

public class PlayerController : MonoBehaviour
{
    void Update()
    {
        // 持续按住
        if (InputManager.Instance.GetKey("Jump"))
        {
            HoldJump();
        }

        // 按下瞬间（最常用）
        if (InputManager.Instance.GetKeyDown("Fire"))
        {
            Shoot();
        }

        // 松开瞬间
        if (InputManager.Instance.GetKeyUp("Reload"))
        {
            FinishReload();
        }
    }
}
```

---

## 核心概念

### 静态按键 vs 动态按键

```
┌─────────────────────────────────────────────────┐
│                  InputManager                    │
│                                                  │
│  ┌──────────────────┐   ┌──────────────────┐    │
│  │  staticBindings  │   │ dynamicBindings  │    │
│  │                  │   │                  │    │
│  │ Jump    → Space  │   │ Skill_0 → Alpha1 │    │
│  │ Fire    → Mouse0 │   │ Skill_1 → Alpha2 │    │
│  │ Reload  → R      │   │ Slot_0  → Q      │    │
│  └──────────────────┘   └──────────────────┘    │
│         ↑                       ↑                │
│    从 SO 加载             运行时 API 创建         │
│    可重置默认值            有独立默认值           │
│    不修改 SO              不修改 SO              │
└─────────────────────────────────────────────────┘
```

| 特性 | 静态按键 | 动态按键 |
|------|---------|---------|
| 创建方式 | Inspector 配置 SO | `AddDynamicAction()` |
| 删除方式 | 修改 SO | `RemoveDynamicAction()` |
| 修改 SO？ | 否 | 否 |
| 重置默认 | ✅ | ✅ |
| 持久化 | ✅ | ✅ |
| 冲突检测 | ✅ | ✅ |

### 动作命名规则

- 静态按键：名称与 SO 中的 `KeyConfig.name` 一致，如 `"Jump"`、`"Fire"`
- 动态按键：格式为 `"{分类}_{名称}"`，如 `"Skills_Fireball"`、`"Inventory_Slot_0"`

---

## API 参考

### InputManager

#### 属性

```csharp
// 获取配置引用
InputConfig Config { get; }
```

#### 动态按键管理

```csharp
// 添加动态按键，返回完整动作名
// category: 分类名（如 "Skills"）
// actionName: 动作名（如 "Fireball"）
// defaultKey: 默认按键
// 返回值: 完整动作名（如 "Skills_Fireball"）
string AddDynamicAction(string category, string actionName, KeyCode defaultKey)

// 移除动态按键
void RemoveDynamicAction(string actionName)

// 判断动作类型
bool IsDynamicAction(string actionName)
bool IsStaticAction(string actionName)
```

#### 改键

```csharp
// 重新绑定按键（InputBinding 版本）
// checkConflicts: 是否检测冲突，默认 true
void RebindKey(string name, InputBinding newBinding, bool checkConflicts = true)

// 重新绑定按键（KeyCode 版本，快捷方式）
void RebindKey(string name, KeyCode newKey, bool checkConflicts = true)

// 取消绑定（设为 KeyCode.None）
void UnbindKey(string name)
```

#### 重置

```csharp
// 重置所有按键到默认值（静态+动态）
void ResetToDefault()

// 重置单个按键到默认值
void ResetKeyToDefault(string name)
```

#### 冲突检测

```csharp
// 检测绑定是否已被其他动作使用
// excludeAction: 排除的动作名（改键时排除自身）
bool IsBindingAlreadyBound(InputBinding binding, string excludeAction = null)

// 检测按键是否已被使用（快捷方式）
bool IsKeyAlreadyBound(KeyCode key, string excludeAction = null)

// 获取使用该绑定的动作名
string GetActionNameByBinding(InputBinding binding)
string GetActionNameByKey(KeyCode key)
```

#### 输入检测

```csharp
// 持续按住
bool GetKey(string name)

// 按下瞬间
bool GetKeyDown(string name)

// 松开瞬间
bool GetKeyUp(string name)
```

#### 查询

```csharp
// 获取绑定信息
InputBinding GetBinding(string name)
KeyCode GetKeyCode(string name)

// 获取所有动作名
List<string> GetAllActionNames()
List<string> GetDynamicActionNames()
List<string> GetStaticActionNames()

// 获取所有绑定
Dictionary<string, InputBinding> GetAllBindings()
```

#### 持久化

```csharp
// 保存到文件（自动调用，也可手动调用）
void SaveBindings()

// 从文件加载
void LoadBindings()

// 删除存档
void ClearSavedBindings()
```

#### 事件

```csharp
// 按键被重新绑定
event Action<string, InputBinding> OnKeyRebinded

// 动态按键被添加
event Action<string> OnActionAdded

// 动态按键被移除
event Action<string> OnActionRemoved
```

### InputBinding

```csharp
// 属性
KeyCode key;           // 主按键
ModifierKey modifiers; // 修饰键（Ctrl/Alt/Shift 可组合）
KeyCode comboKey;      // 组合键（双键组合）
bool IsEmpty;          // 是否为空绑定

// 静态方法
static InputBinding FromKeyCode(KeyCode keyCode)
static ModifierKey GetHeldModifiers()
static bool IsModifierKeyCode(KeyCode key)
static ModifierKey KeyCodeToModifier(KeyCode key)

// 实例方法
bool IsPressed()             // 是否持续按住
bool WasPressedThisFrame()   // 是否本帧按下
bool WasReleasedThisFrame()  // 是否本帧松开
string GetDisplayName()      // 显示名（如 "Ctrl + W"）
```

### ModifierKey

```csharp
[Flags]
public enum ModifierKey
{
    None    = 0,
    Control = 1 << 0,  // 1
    Alt     = 1 << 1,  // 2
    Shift   = 1 << 2,  // 4
    // 可以组合: Control | Shift = 5
}
```

### KeyBindingUI

```csharp
// 序列化字段
string actionName;           // 监听的动作名
Text keyText;                // 显示按键的 Text
Button bindButton;           // 触发改键的 Button
KeyCode[] ignoredKeys;       // 忽略的按键列表

// 事件
UnityEvent<string, InputBinding> onKeyRebinded;
UnityEvent<bool> onListeningStateChanged;

// 方法
void SetActionName(string newActionName)  // 更换监听的动作
void StartListening()                      // 开始监听按键
void CancelListening()                     // 取消监听
void UpdateKeyDisplay()                    // 刷新显示
void ResetToDefault()                      // 重置到默认
void UnbindKey()                           // 取消绑定
```

---

## 使用示例

### 基础：检测输入

```csharp
void Update()
{
    if (InputManager.Instance.GetKeyDown("Jump"))
    {
        player.Jump();
    }

    if (InputManager.Instance.GetKeyDown("Fire"))
    {
        player.Shoot();
    }
}
```

### 改键

```csharp
// 简单改键
InputManager.Instance.RebindKey("Jump", KeyCode.W);

// 带修饰键的改键
var binding = new InputBinding
{
    key = KeyCode.S,
    modifiers = ModifierKey.Control,
};
InputManager.Instance.RebindKey("Save", binding);

// 取消绑定
InputManager.Instance.UnbindKey("Reload");
```

### 冲突检测

```csharp
// 改键前先检查
if (InputManager.Instance.IsKeyAlreadyBound(KeyCode.E))
{
    string conflictAction = InputManager.Instance.GetActionNameByKey(KeyCode.E);
    Debug.Log($"按键 E 已被 {conflictAction} 使用");
}
else
{
    InputManager.Instance.RebindKey("Interact", KeyCode.E);
}
```

### 动态按键：技能系统

```csharp
public class SkillManager : MonoBehaviour
{
    private List<string> skillActions = new List<string>();

    // 学习新技能
    public void LearnSkill(string skillName)
    {
        string actionName = InputManager.Instance.AddDynamicAction(
            "Skills",
            skillName,
            KeyCode.None  // 默认未绑定，让玩家自己设
        );
        skillActions.Add(actionName);

        // 创建对应的 KeyBindingUI...
    }

    // 遗忘技能
    public void ForgetSkill(string skillName)
    {
        string actionName = $"Skills_{skillName}";
        InputManager.Instance.RemoveDynamicAction(actionName);
        skillActions.Remove(actionName);

        // 销毁对应的 KeyBindingUI...
    }

    void Update()
    {
        foreach (var action in skillActions)
        {
            if (InputManager.Instance.GetKeyDown(action))
            {
                UseSkill(action);
            }
        }
    }
}
```

### 动态按键：物品栏槽位

```csharp
public class InventoryHotbar : MonoBehaviour
{
    private const int MAX_SLOTS = 10;

    void Start()
    {
        // 初始化 5 个槽位
        for (int i = 0; i < 5; i++)
        {
            KeyCode defaultKey = KeyCode.Alpha1 + i;
            InputManager.Instance.AddDynamicAction("Hotbar", $"Slot_{i}", defaultKey);
        }
    }

    // 扩展槽位
    public void AddSlot()
    {
        int count = InputManager.Instance.GetDynamicActionNames()
            .FindAll(n => n.StartsWith("Hotbar_")).Count;

        if (count < MAX_SLOTS)
        {
            InputManager.Instance.AddDynamicAction("Hotbar", $"Slot_{count}", KeyCode.None);
        }
    }

    // 使用物品
    void Update()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (InputManager.Instance.GetKeyDown($"Hotbar_Slot_{i}"))
            {
                UseItem(i);
            }
        }
    }
}
```

### UI：动态创建改键面板

```csharp
public class DynamicBindingPanel : MonoBehaviour
{
    [SerializeField] private GameObject bindingUIPrefab;
    [SerializeField] private Transform contentParent;

    private Dictionary<string, KeyBindingUI> uiElements = new();

    void Start()
    {
        InputManager.Instance.OnActionAdded += CreateBindingUI;
        InputManager.Instance.OnActionRemoved += DestroyBindingUI;

        // 为现有动态按键创建 UI
        foreach (var name in InputManager.Instance.GetDynamicActionNames())
        {
            CreateBindingUI(name);
        }
    }

    void CreateBindingUI(string actionName)
    {
        var obj = Instantiate(bindingUIPrefab, contentParent);
        var ui = obj.GetComponent<KeyBindingUI>();
        ui.SetActionName(actionName);
        uiElements[actionName] = ui;
    }

    void DestroyBindingUI(string actionName)
    {
        if (uiElements.TryGetValue(actionName, out var ui))
        {
            Destroy(ui.gameObject);
            uiElements.Remove(actionName);
        }
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnActionAdded -= CreateBindingUI;
            InputManager.Instance.OnActionRemoved -= DestroyBindingUI;
        }
    }
}
```

### 重置

```csharp
// 重置全部
InputManager.Instance.ResetToDefault();

// 重置单个
InputManager.Instance.ResetKeyToDefault("Jump");

// 动态按键也可以重置
InputManager.Instance.ResetKeyToDefault("Skills_Fireball");
```

---

## 高级用法

### 自定义 KeyBindingUI

```csharp
// 通过 UnityEvent 响应改键
public class CustomBindingUI : MonoBehaviour
{
    [SerializeField] private KeyBindingUI bindingUI;
    [SerializeField] private Text conflictWarning;

    void Start()
    {
        bindingUI.onKeyRebinded.AddListener((name, binding) =>
        {
            Debug.Log($"{name} 已改为 {binding.GetDisplayName()}");
        });

        bindingUI.onListeningStateChanged.AddListener((isListening) =>
        {
            // 改键时禁用其他 UI
            pauseMenu.SetActive(!isListening);
        });
    }
}
```

### 序列化数据格式

按键自动保存到 `Application.persistentDataPath/keybindings.json`：

```json
{
    "bindings": [
        {
            "name": "Jump",
            "keyCode": 32,
            "modifiers": 0,
            "comboKey": 0,
            "isDynamic": false
        },
        {
            "name": "Skills_Fireball",
            "keyCode": 49,
            "modifiers": 4,
            "comboKey": 0,
            "isDynamic": true
        }
    ],
    "dynamicDefaults": [
        {
            "name": "Skills_Fireball",
            "defaultKeyCode": 49
        }
    ]
}
```

### 手动控制保存时机

```csharp
// 默认每次操作自动保存，如需批量操作后统一保存：
InputManager.Instance.RebindKey("Jump", KeyCode.W);
InputManager.Instance.RebindKey("Fire", KeyCode.E);
InputManager.Instance.RebindKey("Reload", KeyCode.Q);
InputManager.Instance.SaveBindings();  // 手动保存一次即可
```

---

## 常见问题

### Q: 动态按键会修改 ScriptableObject 吗？

**不会。** 动态按键存储在内存字典中，只持久化到 JSON 文件，ScriptableObject 始终只读。

### Q: 如何区分静态和动态按键？

```csharp
InputManager.Instance.IsStaticAction("Jump")     // true（SO 配置）
InputManager.Instance.IsDynamicAction("Skills_0") // true（运行时创建）
```

### Q: 动态按键能重置吗？

**能。** 创建时记录的默认值会自动持久化，`ResetKeyToDefault` 对动态按键同样有效。

### Q: 如何防止玩家把重要按键取消绑定？

在 `KeyBindingUI` 的 `ignoredKeys` 中添加 `KeyCode.Escape` 等关键按键。也可以监听 `OnKeyRebinded` 事件进行业务层校验。

### Q: 运行时创建的动态按键，重启游戏后还在吗？

**在。** 所有按键（静态+动态）都保存到 `keybindings.json`，启动时自动加载。

### Q: 可以监听手柄输入吗？

当前版本只支持键盘和鼠标。如需手柄支持，可以扩展 `InputBinding` 添加设备类型字段。

### Q: GetKeyDown 和 Input.GetKeyDown 有什么区别？

`InputManager.Instance.GetKeyDown("Jump")` 会根据当前绑定自动判断按键，并且支持修饰键和组合键检测。

---

## 依赖

- Unity 2019.4 或更高版本
- 无需其他插件或包

---

## 许可证

MIT License