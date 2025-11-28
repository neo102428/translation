# Mutex 释放问题修复

## ?? 问题描述

### 症状
当运行已发布的 `translation.exe` 后，再次运行程序会弹出错误对话框：

```
UI 线程异常:
Object synchronization method was called from an unsynchronized block of code.

at System.Threading.Mutex.ReleaseMutex()
at translation.App.OnExit(ExitEventArgs e) in D:\code\translation\App.xaml.cs:line 201
```

### 根本原因

**线程同步错误**：第二个实例尝试释放它并不拥有的 Mutex。

#### 问题流程：

1. **第一个实例启动**
   - 创建 Mutex：`new Mutex(true, appName, out createdNew)`
   - `createdNew = true` → 拥有 Mutex
   - 正常运行

2. **第二个实例启动**
   - 尝试创建 Mutex：`new Mutex(true, appName, out createdNew)`
   - `createdNew = false` → 没有拥有 Mutex（第一个实例已拥有）
   - 检测到已有实例运行
   - 调用 `Application.Current.Shutdown()`
   - 触发 `OnExit` 事件

3. **第二个实例退出时**
   ```csharp
   protected override void OnExit(ExitEventArgs e)
   {
       _mutex?.ReleaseMutex();  // ? 错误！没有拥有 Mutex 却尝试释放
       _mutex?.Dispose();
   }
   ```
   - 尝试 `ReleaseMutex()` 但它不拥有这个 Mutex
   - 抛出异常：`Object synchronization method was called from an unsynchronized block of code`

---

## ? 解决方案

### 核心思路
**只有拥有 Mutex 的实例才能释放它**

### 代码修改

#### 1. 添加标志变量

```csharp
private static Mutex _mutex = null;
private static bool _mutexOwned = false; // 新增：标记是否拥有 Mutex
```

#### 2. 在创建 Mutex 时记录所有权

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    const string appName = "Global\\TranslationTool_Mutex_2A8A2E8D_8B4E_4C4F_A8D7_3B6C9B0E1F2A";
    bool createdNew;

    _mutex = new Mutex(true, appName, out createdNew);
    _mutexOwned = createdNew; // ? 记录是否拥有 Mutex

    if (!createdNew)
    {
        // 第二个实例：不拥有 Mutex
        MessageBox.Show("划词翻译工具已经在运行中！...", ...);
        
        _mutex?.Close();  // ? 只关闭，不释放
        _mutex = null;
        
        Application.Current.Shutdown();
        return;
    }

    // 第一个实例：拥有 Mutex，继续初始化...
}
```

#### 3. 在退出时有条件地释放

```csharp
protected override void OnExit(ExitEventArgs e)
{
    _notifyIcon?.Dispose();
    _mouseHook?.Dispose();
    
    // ? 只有拥有 Mutex 时才释放
    if (_mutexOwned && _mutex != null)
    {
        try
        {
            _mutex.ReleaseMutex();
        }
        catch
        {
            // 忽略释放错误
        }
    }
    
    // 总是 Dispose Mutex
    _mutex?.Dispose();
    
    base.OnExit(e);
}
```

---

## ?? 修复前后对比

### 修复前

| 实例 | 拥有 Mutex | OnExit 行为 | 结果 |
|------|-----------|-------------|------|
| 第一个 | ? 是 | ReleaseMutex() | ? 正常 |
| 第二个 | ? 否 | ReleaseMutex() | ? 异常 |

### 修复后

| 实例 | 拥有 Mutex | OnExit 行为 | 结果 |
|------|-----------|-------------|------|
| 第一个 | ? 是 | ReleaseMutex() + Dispose() | ? 正常 |
| 第二个 | ? 否 | Close() + Dispose() | ? 正常 |

---

## ?? 测试验证

### 测试场景 1: 正常单实例运行
1. 启动 `translation.exe`
2. 程序正常运行
3. 退出程序
4. **预期结果**：? 无错误，正常退出

### 测试场景 2: 尝试启动第二个实例
1. 启动第一个 `translation.exe`
2. 再次双击 `translation.exe`
3. **预期结果**：
   - ? 弹出提示："划词翻译工具已经在运行中！..."
   - ? 第二个实例自动关闭
   - ? **不再出现** "Object synchronization method..." 错误

### 测试场景 3: 第一个实例继续运行
1. 启动第一个实例
2. 尝试启动第二个实例（自动关闭）
3. 第一个实例继续正常工作
4. 退出第一个实例
5. **预期结果**：? 第一个实例正常退出，无错误

---

## ?? 技术细节

### Mutex 的所有权规则

在 .NET 中，`Mutex` 有明确的所有权规则：

1. **创建 Mutex**
   ```csharp
   var mutex = new Mutex(initiallyOwned: true, name: "UniqueName", out bool createdNew);
   ```
   - 如果 `createdNew = true`：当前线程拥有 Mutex
   - 如果 `createdNew = false`：另一个进程/线程已拥有此名称的 Mutex

2. **释放 Mutex**
   ```csharp
   mutex.ReleaseMutex();
   ```
   - ?? **只有拥有 Mutex 的线程才能释放它**
   - 如果没有拥有却尝试释放 → 抛出 `ApplicationException`

3. **关闭和 Dispose**
   ```csharp
   mutex.Close();    // 关闭句柄，不释放所有权
   mutex.Dispose();  // 释放资源（内部会调用 Close）
   ```
   - 任何持有 Mutex 引用的代码都可以调用
   - 即使没有所有权也可以安全调用

### 最佳实践

#### ? 推荐做法

```csharp
// 1. 记录所有权
bool mutexOwned = false;
mutex = new Mutex(true, name, out mutexOwned);

// 2. 有条件地释放
if (mutexOwned && mutex != null)
{
    try
    {
        mutex.ReleaseMutex();
    }
    catch { }
}

// 3. 总是 Dispose
mutex?.Dispose();
```

#### ? 错误做法

```csharp
// 错误 1: 不检查所有权就释放
mutex?.ReleaseMutex();  // 可能抛出异常

// 错误 2: 忘记 Dispose
mutex.ReleaseMutex();   // 释放了但没有清理资源

// 错误 3: 多次释放
mutex.ReleaseMutex();
mutex.ReleaseMutex();   // 错误！
```

---

## ?? 总结

### 修复内容
1. ? 添加 `_mutexOwned` 标志记录所有权
2. ? 第二个实例检测到重复后，只 `Close()` 不 `ReleaseMutex()`
3. ? `OnExit` 中有条件地释放 Mutex
4. ? 添加 try-catch 保护释放操作

### 修复效果
- ? 第一个实例正常运行和退出
- ? 第二个实例检测到重复，友好提示，安全退出
- ? 不再出现"Object synchronization method..."错误
- ? 系统资源正确清理

### 学到的教训
- ?? Mutex 有严格的所有权规则
- ?? 只有拥有者才能释放 Mutex
- ?? 即使没有所有权也要 Dispose
- ?? 使用标志变量跟踪所有权状态

---

现在程序可以正确处理多实例启动的情况了！??
