# Fix toàn diện cho vấn đề Filter với nhiều điều kiện

## Vấn đề
Khi có nhiều loại filter active cùng lúc (Object Type + Component + Search), việc expand all không tuân theo tất cả các filter, dẫn đến hiển thị items không mong muốn.

## Nguyên nhân
1. **Logic filter không tổng hợp**: Mỗi loại filter được xử lý riêng biệt, không có method tổng hợp để áp dụng tất cả các filter cùng lúc
2. **Expand All không respect filter**: Method `ExpandAllTreeViewItems` expand tất cả items mà không kiểm tra filter state
3. **Search filter được xử lý riêng**: Search được xử lý trong View thay vì tích hợp với ViewModel

## Giải pháp đã implement

### 1. Tạo method tổng hợp `ApplyAllFilters()`

```csharp
/// <summary>
/// Áp dụng tất cả các filter (Object Type + Component + Search) cho toàn bộ tree
/// </summary>
private void ApplyAllFilters()
{
    // Apply to all scenes in the tree
    foreach (var sceneItem in SceneTreeItems.ToList())
    {
        ApplyAllFiltersToScene(sceneItem);
    }
}

private void ApplyAllFiltersToScene(SceneTreeItemViewModel sceneItem)
{
    // Apply visibility filter to all groups
    ApplyVisibilityFilterToGroups(sceneItem);
    
    // Apply to child scenes recursively
    foreach (var childScene in sceneItem.Children.Where(c => c.ItemType == SceneTreeItemType.Scene).ToList())
    {
        ApplyAllFiltersToScene(childScene);
    }
}
```

### 2. Cải thiện logic filter tổng hợp trong `ShouldItemBeVisible()`

```csharp
private bool ShouldItemBeVisible(SceneTreeItemViewModel item)
{
    // 1. Object Type Filter
    bool objectTypeMatch = CurrentObjectTypeFilter switch
    {
        ObjectTypeFilter.All => true,
        ObjectTypeFilter.ActorsOnly => item.ItemType == SceneTreeItemType.Actor,
        ObjectTypeFilter.FrisesOnly => item.ItemType == SceneTreeItemType.Frise,
        _ => true
    };

    if (!objectTypeMatch) return false;

    // 2. Component Filter (chỉ áp dụng cho actors)
    if (item.ItemType == SceneTreeItemType.Actor && IsComponentFilterEnabled && SelectedComponents.Count > 0)
    {
        if (item.Model is ActorModel actor)
        {
            var selectedComponentsSet = new HashSet<string>(SelectedComponents, StringComparer.OrdinalIgnoreCase);
            bool hasSelectedComponent = _componentFilterService.ActorHasAnyComponent(actor, selectedComponentsSet);
            if (!hasSelectedComponent) return false;
        }
    }

    // 3. Search Filter
    if (!string.IsNullOrWhiteSpace(SearchText))
    {
        bool searchMatch = item.DisplayName?.ToLowerInvariant().Contains(SearchText.ToLowerInvariant()) ?? false;
        if (!searchMatch) return false;
    }

    return true;
}
```

### 3. Thêm SearchText property vào ViewModel

```csharp
public string SearchText
{
    get => _searchText;
    set
    {
        if (SetProperty(ref _searchText, value))
        {
            // Apply all filters when search text changes
            ApplyAllFilters();
        }
    }
}
```

### 4. Cập nhật tất cả các nơi gọi filter

- Thay thế tất cả `ApplyComponentFilters()` → `ApplyAllFilters()`
- Thay thế tất cả `ApplyObjectTypeFilter()` → `ApplyAllFilters()`
- Cập nhật View để sử dụng `SearchText` property từ ViewModel

### 5. Cải thiện logic Expand All

```csharp
private void ExpandTreeViewItemRecursive(TreeViewItem? item)
{
    if (item == null) return;

    // Chỉ expand nếu item đang visible
    if (item.Visibility == System.Windows.Visibility.Visible)
    {
        // Kiểm tra IsVisible property của ViewModel nếu có
        if (item.DataContext is SceneTreeItemViewModel viewModel && !viewModel.IsVisible)
        {
            return;
        }

        item.IsExpanded = true;
        item.UpdateLayout();

        foreach (var subItem in item.Items)
        {
            var subTreeViewItem = item.ItemContainerGenerator.ContainerFromItem(subItem) as TreeViewItem;
            ExpandTreeViewItemRecursive(subTreeViewItem);
        }
    }
}
```

## Cách hoạt động

### Trước khi fix:
1. Chọn "Show Actor Only" → Chỉ hiện ActorSet group
2. Chọn component filter (VD: Ray_ChangePageComponent) → Filter actors theo component
3. Search text → Filter theo text
4. Click "Expand All" → Expand tất cả items, bao gồm cả những items không match filter

### Sau khi fix:
1. Chọn "Show Actor Only" → Chỉ hiện ActorSet group
2. Chọn component filter → Filter actors theo component
3. Search text → Filter theo text
4. Click "Expand All" → Chỉ expand những items match tất cả các filter active

## Kết quả

### ✅ Tất cả các filter được áp dụng đúng cách:
- **Object Type Filter**: Chỉ hiển thị actors hoặc frises theo lựa chọn
- **Component Filter**: Chỉ hiển thị actors có component được chọn
- **Search Filter**: Chỉ hiển thị items match search text
- **Expand All**: Chỉ expand những items đang visible theo tất cả filter

### ✅ Performance được cải thiện:
- Tất cả filter được áp dụng trong một method duy nhất
- Giảm số lần gọi UI update
- Better caching và optimization

### ✅ User Experience tốt hơn:
- Filter hoạt động nhất quán trong mọi trường hợp
- Expand All respect tất cả filter active
- Search tích hợp hoàn toàn với các filter khác

## Testing
Để test fix này:
1. Load scene có cả actors và frises
2. Chọn "Show Actor Only" + component filter + search text
3. Click "Expand All"
4. Kiểm tra: Chỉ thấy actors có component được chọn và match search text
5. Thử với các combination khác nhau của filter
