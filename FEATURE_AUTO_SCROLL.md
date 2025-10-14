# ✨ Feature: Auto-Scroll to Selected Item in Scene Explorer

## Mô tả tính năng

Khi user select một object từ engine, Scene Explorer sẽ:
1. ✅ Tìm và highlight item tương ứng trong tree
2. ✅ Expand parent hierarchy để item hiển thị
3. ✅ **Tự động scroll để đưa item vào giữa màn hình** (UPDATED: Queue + multi-attempt, layout-driven)

## Implementation

### 1. Thêm Event trong ViewModel
**File:** `SceneExplorerViewModel.cs`

```csharp
public class SceneExplorerViewModel : SubToolViewModelBase, IDisposable
{
    // Events
    public event EventHandler<SceneTreeItemViewModel>? ScrollToItemRequested;
    
    // ...
}
```

### 2. Raise Event khi Object được Select
**File:** `SceneExplorerViewModel.cs`

```csharp
private void OnObjectSelectedFromRuntime(object? sender, uint objectRef)
{
    App.Current?.Dispatcher.Invoke(() =>
    {
        var selectedItem = FindTreeItemByObjectRef(SceneTreeItems, objectRef);
        if (selectedItem != null)
        {
            // Clear selection
            ClearTreeSelection(SceneTreeItems);
            
            // Set new selection
            selectedItem.IsSelected = true;
            selectedItem.IsExpanded = true;
            
            // Expand parent hierarchy
            ExpandParentHierarchy(selectedItem);
            
            // Update SelectedObject
            if (selectedItem.Model is ObjectWithRefModel objectModel)
            {
                SelectedObject = objectModel;
            }

            // ✨ NEW: Request scroll to center the item
            RequestScrollToItem(selectedItem);
        }
    });
}

private void RequestScrollToItem(SceneTreeItemViewModel item)
{
    // Small delay to ensure layout is updated
    Task.Delay(100).ContinueWith(_ =>
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            ScrollToItemRequested?.Invoke(this, item);
            LogService.Info($"📜 Requested scroll to item: {item.DisplayName}");
        });
    });
}
```

### 3. Subscribe to Event trong View
**File:** `SceneExplorerView.xaml.cs`

```csharp
public SceneExplorerView()
{
    InitializeComponent();
    
    // ... existing code ...
    
    // Subscribe to scroll request from ViewModel
    this.DataContextChanged += SceneExplorerView_DataContextChanged;
}

private void SceneExplorerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
{
    // Unsubscribe from old ViewModel
    if (e.OldValue is SceneExplorerViewModel oldViewModel)
    {
        oldViewModel.ScrollToItemRequested -= OnScrollToItemRequested;
    }

    // Subscribe to new ViewModel
    if (e.NewValue is SceneExplorerViewModel newViewModel)
    {
        newViewModel.ScrollToItemRequested += OnScrollToItemRequested;
    }
}

private void OnScrollToItemRequested(object? sender, SceneTreeItemViewModel item)
{
    ScrollToTreeViewItem(item);
}
```

### 4. Implement Smooth Scroll Logic (Initial Version)

**File:** `SceneExplorerView.xaml.cs`

```csharp
private void ScrollToTreeViewItem(SceneTreeItemViewModel item)
{
    // Force layout update
    SceneTreeView.UpdateLayout();

    // Find the TreeViewItem container
    var treeViewItem = FindTreeViewItem(SceneTreeView, item);
    if (treeViewItem != null)
    {
        // Bring into view
        treeViewItem.BringIntoView();

        // Center the item with animation
        Dispatcher.BeginInvoke(new Action(() =>
        {
            CenterTreeViewItem(treeViewItem);
        }), DispatcherPriority.Loaded);
    }
}

private void CenterTreeViewItem(TreeViewItem item)
{
    var scrollViewer = FindVisualChild<ScrollViewer>(SceneTreeView);
    if (scrollViewer == null) return;

    // Get item position
    var transform = item.TransformToAncestor(scrollViewer);
    var itemPosition = transform.Transform(new Point(0, 0));

    // Calculate center position
    var viewportHeight = scrollViewer.ViewportHeight;
    var itemHeight = item.ActualHeight;
    var targetOffset = itemPosition.Y - (viewportHeight / 2) + (itemHeight / 2);
    
    // Clamp to valid range
    targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

    // Smooth scroll animation
    AnimateScroll(scrollViewer, targetOffset);
}

private void AnimateScroll(ScrollViewer scrollViewer, double targetOffset)
{
    var currentOffset = scrollViewer.VerticalOffset;
    var distance = targetOffset - currentOffset;
    var duration = TimeSpan.FromMilliseconds(300); // 300ms smooth animation
    var startTime = DateTime.Now;

    var timer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
    };

    timer.Tick += (s, e) =>
    {
        var elapsed = DateTime.Now - startTime;
        var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);
        
        // Ease out cubic for smooth deceleration
        var easedProgress = 1 - Math.Pow(1 - progress, 3);
        
        var newOffset = currentOffset + (distance * easedProgress);
        scrollViewer.ScrollToVerticalOffset(newOffset);

        if (progress >= 1.0)
        {
            timer.Stop();
        }
    };

    timer.Start();
}
```

## Đặc điểm

### ✨ Smooth Animation

- Sử dụng **ease-out cubic** function cho chuyển động mượt mà
- Animation duration: **300ms**
- Frame rate: **~60 FPS** (16ms interval)

### 📍 Smart Centering

- Tự động tính toán vị trí để đưa item vào **giữa viewport**
- Xử lý edge cases (item ở đầu/cuối danh sách)
- Clamp scroll offset trong phạm vi hợp lệ

### ⏱️ Delayed Execution (Updated)

- Ban đầu delay 100ms; nay tăng lên 300ms trong `RequestScrollToItem` để đảm bảo TreeView containers đã sinh ra với cây lớn.
- Thêm cơ chế **Queue + LayoutUpdated retry** tối đa 8 attempts (DispatcherPriority.Background) nhằm xử lý trường hợp container chưa kịp generate dù đã delay.
- Không còn dùng `Thread.Sleep` (tránh block UI); thay bằng dispatcher re-queue và lắng nghe LayoutUpdated.

### 🔍 Robust Item Finding (Enhanced)

- Recursive search + auto-expand node ancestors (được thực hiện sẵn trong ViewModel bằng `ExpandParentHierarchy`).
- LayoutUpdated-based retry cho đến khi `ItemContainerGenerator` cung cấp đúng `TreeViewItem`.
- Giới hạn attempts để tránh vòng lặp vô hạn (Max 8). Có log debug khi bỏ cuộc.

### ♻️ Queue + Retry Cơ Chế Mới

1. ViewModel gọi `RequestScrollToItem` (delay 300ms) -> raise event
2. View nhận event và `QueueScrollToItem(item)` đặt `_pendingScrollItem`
3. Gắn handler `LayoutUpdated` (nếu chưa gắn)
4. Mỗi lần layout update hoặc dispatcher background tick -> `AttemptScroll()`
5. Nếu tìm được container: BringIntoView + center (animation) rồi tháo handler
6. Nếu chưa: re-queue attempt tới khi đạt Max hoặc thành công

Lợi ích: Loại bỏ timing race giữa selection, expansion, và container generation khi tree lớn hoặc UI bận.

## User Experience Flow

```text
1. User clicks object in Engine
   ↓
2. Engine sends selection event
   ↓
3. SceneExplorerViewModel receives event
   ↓
4. Find item in tree → Set IsSelected → Expand parents
   ↓
5. Raise ScrollToItemRequested event
   ↓
6. SceneExplorerView receives event
   ↓
7. Find TreeViewItem container
   ↓
8. Calculate center position
   ↓
9. Animate smooth scroll to center
   ↓
10. ✨ Item is centered in viewport!
```

## Benefits

✅ **Better UX:** User luôn thấy item được select ở vị trí dễ nhìn
✅ **Smooth Animation:** Không bị "jump" đột ngột
✅ **Smart Positioning:** Tự động center item trong viewport
✅ **Reliable:** Xử lý tốt với nested tree và large hierarchies
✅ **Performance:** Smooth 60 FPS animation

## Testing Scenarios (Updated)

- [x] Select object từ engine → scroll to center ✅
- [x] Select object ở đầu tree → scroll mượt ✅
- [x] Select object ở cuối tree → scroll mượt ✅
- [x] Select nested object → expand + scroll ✅
- [x] Rapid selection changes (liên tiếp nhiều event) → chỉ item cuối cùng scroll (pending item override) ✅
- [x] Large tree (deep hierarchy) → container found trong <= 8 attempts ✅
- [ ] Stress test với >10k nodes (khuyến nghị bật virtualization sau nếu cần) ⏳

## Technical Details

### Dependencies

- `System.Windows.Controls.TreeView`
- `System.Windows.Controls.ScrollViewer`
- `System.Windows.Threading.DispatcherTimer`
- `System.Windows.Media.VisualTreeHelper`

### Key Methods

- `ScrollToTreeViewItem()` - Main entry point
- `FindTreeViewItem()` - Recursive item finder
- `CenterTreeViewItem()` - Calculate center position
- `AnimateScroll()` - Smooth scroll animation
- `FindVisualChild<T>()` - Visual tree helper

### Animation Math

```csharp
// Ease out cubic
easedProgress = 1 - Math.Pow(1 - progress, 3)

// Target offset
targetOffset = itemY - (viewportHeight / 2) + (itemHeight / 2)

// Current offset with easing
newOffset = startOffset + (distance * easedProgress)
```

---

**Implemented by:** GitHub Copilot  
**Initial Date:** October 15, 2025  
**Latest Update:** October 15, 2025 (Reliability queue + retry)  
**Files Modified (Latest):**

- `SceneExplorerViewModel.cs` - Increased delay 300ms
- `SceneExplorerView.xaml.cs` - Added QueueScrollToItem, AttemptScroll, LayoutUpdated retry logic, removed Thread.Sleep
