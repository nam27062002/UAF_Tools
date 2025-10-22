# Fix cho vấn đề Filter "Show Actor Only" vẫn hiển thị Frises khi Expand All

## Vấn đề
Khi chọn "Show Actor Only" filter và sau đó click "Expand All", vẫn thấy có những frises hiển thị trong tree view.

## Nguyên nhân
1. **Logic filter không đầy đủ**: `ApplyObjectTypeFilterToScene` chỉ ẩn/hiện các group (ActorSet/FriseSet) nhưng không set visibility cho các individual items bên trong group
2. **Expand All không respect filter**: Method `ExpandAllTreeViewItems` expand tất cả TreeViewItem mà không kiểm tra filter state
3. **Visibility không được áp dụng đúng cách**: Các items bên trong group vẫn có `IsVisible = true` mặc dù filter đang active

## Giải pháp đã implement

### 1. Cải thiện logic filter trong SceneExplorerViewModel.cs

#### Thêm method `ApplyVisibilityFilterToGroups`:
```csharp
private void ApplyVisibilityFilterToGroups(SceneTreeItemViewModel sceneItem)
{
    foreach (var group in sceneItem.Children.Where(c => 
        c.ItemType == SceneTreeItemType.ActorSet || 
        c.ItemType == SceneTreeItemType.FriseSet))
    {
        foreach (var item in group.Children)
        {
            bool shouldBeVisible = CurrentObjectTypeFilter switch
            {
                ObjectTypeFilter.All => true,
                ObjectTypeFilter.ActorsOnly => item.ItemType == SceneTreeItemType.Actor,
                ObjectTypeFilter.FrisesOnly => item.ItemType == SceneTreeItemType.Frise,
                _ => true
            };

            item.IsVisible = shouldBeVisible;
        }
    }
}
```

#### Cập nhật `ApplyObjectTypeFilterToScene`:
- Thêm gọi `ApplyVisibilityFilterToGroups(sceneItem)` sau khi thêm/bỏ groups
- Đảm bảo visibility được áp dụng cho từng item trong group

### 2. Cải thiện logic Expand All trong SceneExplorerView.xaml.cs

#### Tạo method `ExpandTreeViewItemRecursive` mới:
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

#### Cập nhật `ExpandAllTreeViewItems`:
- Sử dụng `ExpandTreeViewItemRecursive` thay vì `ExpandTreeViewItem`
- Đảm bảo chỉ expand những item đang visible

## Cách hoạt động

### Trước khi fix:
1. Chọn "Show Actor Only" → Chỉ hiện ActorSet group, ẩn FriseSet group
2. Click "Expand All" → Expand tất cả TreeViewItem, bao gồm cả frises bên trong ActorSet group (nếu có)

### Sau khi fix:
1. Chọn "Show Actor Only" → Chỉ hiện ActorSet group, ẩn FriseSet group
2. `ApplyVisibilityFilterToGroups` set `IsVisible = false` cho tất cả frises
3. Click "Expand All" → Chỉ expand những item có `IsVisible = true`
4. Kết quả: Chỉ thấy actors, không thấy frises

## Testing
Để test fix này:
1. Load một scene có cả actors và frises
2. Chọn "Show Actor Only" filter
3. Click "Expand All"
4. Kiểm tra: Chỉ thấy actors, không thấy frises nào
5. Thử với "Show Frises Only" và "Show All" để đảm bảo hoạt động đúng

## Kết quả
- Filter "Show Actor Only" hoạt động đúng cách
- Expand All chỉ expand những item đang visible theo filter
- Không còn hiển thị frises khi filter "Show Actor Only" active
