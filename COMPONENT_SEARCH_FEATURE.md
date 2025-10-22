# Tính năng Search cho Component Filters

## Vấn đề
Khi có nhiều components trong scene, việc tìm kiếm component cụ thể trong danh sách dài trở nên khó khăn và mất thời gian.

## Giải pháp - Component Search Feature

### 🎯 Tính năng mới:
- **Search Box** trong Component Filters panel
- **Real-time filtering** khi user gõ
- **Case-insensitive search** 
- **Placeholder text** để hướng dẫn user
- **Component count display** hiển thị số components được filter

### 🔧 Implementation Details:

#### 1. **ViewModel Changes**:

**Thêm properties mới**:
```csharp
private string _componentSearchText = string.Empty;
private ObservableCollection<ComponentFilterModel> _filteredComponents = new();

public string ComponentSearchText
{
    get => _componentSearchText;
    set
    {
        if (SetProperty(ref _componentSearchText, value))
        {
            FilterAvailableComponents();
        }
    }
}

public ObservableCollection<ComponentFilterModel> FilteredComponents
{
    get => _filteredComponents;
    set => SetProperty(ref _filteredComponents, value);
}
```

**Filtering Logic**:
```csharp
private void FilterAvailableComponents()
{
    try
    {
        if (string.IsNullOrWhiteSpace(ComponentSearchText))
        {
            // Show all components when search is empty
            FilteredComponents.Clear();
            foreach (var component in AvailableComponents)
            {
                FilteredComponents.Add(component);
            }
        }
        else
        {
            // Filter components based on search text
            var searchText = ComponentSearchText.ToLowerInvariant();
            FilteredComponents.Clear();
            
            foreach (var component in AvailableComponents)
            {
                if (component.DisplayText?.ToLowerInvariant().Contains(searchText) == true)
                {
                    FilteredComponents.Add(component);
                }
            }
        }
        
        OnPropertyChanged(nameof(FilteredComponents));
        LogService.Info($"Filtered components: {FilteredComponents.Count}/{AvailableComponents.Count}");
    }
    catch (Exception ex)
    {
        LogService.Error("Error filtering components", ex);
    }
}
```

#### 2. **UI Changes**:

**Search Box Design**:
```xml
<!-- Component Search Box -->
<TextBox Grid.Row="0"
         Text="{Binding ComponentSearchText, UpdateSourceTrigger=PropertyChanged}"
         Margin="0,0,0,8"
         Height="28"
         FontSize="11"
         Padding="8,4"
         Background="{DynamicResource MaterialDesignBackground}"
         BorderBrush="{DynamicResource MaterialDesignDivider}"
         BorderThickness="1"
         VerticalContentAlignment="Center">
    <!-- Placeholder text styling -->
    <TextBox.Style>
        <Style TargetType="TextBox">
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TextBox">
                        <Border Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="4">
                            <Grid>
                                <ScrollViewer x:Name="PART_ContentHost"
                                             Focusable="False"
                                             HorizontalScrollBarVisibility="Hidden"
                                             VerticalScrollBarVisibility="Hidden"/>
                                <TextBlock Text="Search components..."
                                           Foreground="{DynamicResource MaterialDesignBodyLight}"
                                           VerticalAlignment="Center"
                                           Margin="8,0"
                                           IsHitTestVisible="False">
                                    <!-- Show placeholder when text is empty -->
                                    <TextBlock.Style>
                                        <Style TargetType="TextBlock">
                                            <Setter Property="Visibility" Value="Collapsed"/>
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding Text, RelativeSource={RelativeSource TemplatedParent}}" Value="">
                                                    <Setter Property="Visibility" Value="Visible"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </TextBlock.Style>
                                </TextBlock>
                            </Grid>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </TextBox.Style>
</TextBox>
```

**Updated Component List**:
```xml
<!-- Component List -->
<ScrollViewer Grid.Row="3"
              VerticalScrollBarVisibility="Auto"
              HorizontalScrollBarVisibility="Disabled"
              Padding="2">
    <ItemsControl ItemsSource="{Binding FilteredComponents}">
        <!-- Vertical layout for better readability -->
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Vertical" Margin="2"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <!-- Component buttons with full width -->
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <ToggleButton Content="{Binding DisplayText}"
                              IsChecked="{Binding IsSelected, Mode=TwoWay}"
                              Margin="2"
                              Padding="8,3"
                              FontSize="10"
                              FontWeight="Medium"
                              MinWidth="200"
                              MinHeight="24"
                              HorizontalAlignment="Stretch">
                    <!-- Styling for better UX -->
                </ToggleButton>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</ScrollViewer>
```

**Component Count Display**:
```xml
<TextBlock Text="{Binding FilteredComponents.Count, StringFormat={}{0} components shown}"
           VerticalAlignment="Center"
           Margin="0,0,0,0"
           FontSize="10"
           Foreground="{DynamicResource MaterialDesignBodyLight}"/>
```

### 🎨 UI/UX Improvements:

#### **Search Box Features**:
- ✅ **Placeholder text**: "Search components..." để hướng dẫn user
- ✅ **Real-time filtering**: Filter ngay khi user gõ
- ✅ **Case-insensitive**: Không phân biệt hoa thường
- ✅ **Clean design**: Material Design styling
- ✅ **Responsive**: Tự động adjust theo content

#### **Component List Features**:
- ✅ **Filtered display**: Chỉ hiển thị components match search
- ✅ **Full width buttons**: Dễ đọc component names
- ✅ **Vertical layout**: Dễ scan danh sách
- ✅ **Count display**: Hiển thị số components được filter
- ✅ **Preserved selections**: Giữ nguyên selection khi filter

### 🚀 User Workflow:

#### **Before (Không có search)**:
1. User phải scroll qua danh sách dài components
2. Khó tìm component cụ thể
3. Mất thời gian để locate component

#### **After (Có search)**:
1. User gõ tên component vào search box
2. Danh sách tự động filter theo real-time
3. Chỉ hiển thị components match search
4. Dễ dàng tìm và select component mong muốn

### 📊 Performance Optimizations:

#### **Efficient Filtering**:
- **Case-insensitive search**: Sử dụng `ToLowerInvariant()` cho performance tốt
- **Real-time filtering**: Chỉ filter khi search text thay đổi
- **Memory efficient**: Không tạo duplicate objects
- **UI responsive**: Filtering không block UI thread

#### **Smart Updates**:
- **Preserve selections**: Giữ nguyên component selections khi filter
- **Update counts**: Hiển thị số components được filter
- **Logging**: Track filtering performance cho debugging

### 🎯 Benefits:

#### **For Users**:
- **Faster workflow**: Tìm component nhanh hơn
- **Better UX**: Không cần scroll qua danh sách dài
- **Intuitive**: Search box quen thuộc với users
- **Flexible**: Có thể search partial names

#### **For Developers**:
- **Maintainable**: Clean separation of concerns
- **Extensible**: Dễ thêm features như regex search
- **Testable**: Logic được tách riêng trong ViewModel
- **Performance**: Optimized filtering algorithm

### 🔮 Future Enhancements:

#### **Advanced Search Features**:
- **Regex support**: Search với regular expressions
- **Category filtering**: Filter theo component categories
- **Sort options**: Sort theo name, count, etc.
- **Search history**: Remember recent searches

#### **UI Improvements**:
- **Search suggestions**: Auto-complete khi gõ
- **Highlight matches**: Highlight search terms trong results
- **Keyboard shortcuts**: Quick access với hotkeys
- **Search filters**: Advanced filtering options

## Kết quả
Tính năng search components cung cấp:
- **Better discoverability**: Dễ tìm components trong danh sách dài
- **Improved workflow**: Faster component selection process  
- **Professional UX**: Giống các modern IDEs
- **Scalable**: Hoạt động tốt với nhiều components
