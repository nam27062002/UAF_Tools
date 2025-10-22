# Fix cho vấn đề Tool không shutdown hoàn toàn

## Vấn đề
Tool không shutdown hoàn toàn sau khi đóng, vẫn chạy ngầm trong Task Manager do các background threads và services không được stop đúng cách.

## Nguyên nhân chính
1. **Background threads không được cancel**: Các service như `SceneExplorerService`, `PropertiesEditorService`, `EngineIntegrationService` có background threads chạy liên tục
2. **Services không được stop trong App shutdown**: App.xaml.cs chỉ dispose DI host mà không stop các services đang chạy
3. **Thiếu proper cleanup sequence**: Không có thứ tự cleanup đúng cách cho các services

## Giải pháp đã implement

### 1. Cải thiện App.xaml.cs
- **Thêm method `StopAllServices()`**: Stop tất cả services trước khi dispose DI host
- **Stop các async services**: Gọi `StopAsync()` cho các service có background operations
- **Dispose services**: Dispose tất cả services implement IDisposable
- **Tăng thời gian force exit**: Từ 1 giây lên 3 giây để cho phép cleanup hoàn tất

### 2. Cải thiện EnginePluginServiceBase
- **Cải thiện `StopAsync()` method**: 
  - Cancel cancellation token trước
  - Chờ 100ms cho network thread exit gracefully
  - Dispose cancellation token source
  - Disconnect từ engine
- **Cải thiện `Dispose()` method**:
  - Gọi StopAsync() trước
  - Clear connection state
  - Proper logging

### 3. Cải thiện EngineIntegrationService
- **Cải thiện `DisconnectAsync()` method**:
  - Better error handling cho engine disconnect
  - Proper cleanup của engine wrapper
- **Cải thiện `Dispose()` method**:
  - Chờ disconnect hoàn tất thay vì fire-and-forget
  - Proper disposal của cancellation token source

### 4. Cải thiện MainWindow.xaml.cs
- **Thêm garbage collection**: Force GC sau khi cleanup để giúp giải phóng memory
- **Better logging**: Thêm debug logs để track cleanup process

## Các thay đổi chi tiết

### App.xaml.cs
```csharp
private void StopAllServices()
{
    // Stop tất cả services trước khi dispose host
    var servicesToStop = new[]
    {
        typeof(ISceneExplorerService),
        typeof(IPropertiesEditorService), 
        typeof(IEngineIntegrationService),
        typeof(IEngineHostService),
        typeof(IToolManager)
    };

    foreach (var serviceType in servicesToStop)
    {
        // Stop async services và dispose
    }
}
```

### EnginePluginServiceBase.cs
```csharp
public virtual Task StopAsync(CancellationToken cancellationToken = default)
{
    // Cancel cancellation token trước
    _cancellationTokenSource.Cancel();
    
    // Chờ network thread exit gracefully
    Task.Delay(100, cancellationToken).GetAwaiter().GetResult();
    
    // Dispose và cleanup
}
```

## Kết quả mong đợi
- Tool sẽ shutdown hoàn toàn sau khi đóng
- Không còn process chạy ngầm trong Task Manager
- Proper cleanup của tất cả resources và background threads
- Better error handling và logging cho debug

## Testing
Để test fix này:
1. Build và chạy tool
2. Sử dụng tool một lúc
3. Đóng tool
4. Kiểm tra Task Manager - không còn process nào của tool chạy ngầm
5. Check debug output để đảm bảo cleanup sequence hoạt động đúng
