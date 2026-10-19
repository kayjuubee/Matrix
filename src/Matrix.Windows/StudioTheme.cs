using System.Windows;
using System.Windows.Markup;

namespace Matrix.Windows;

internal static class StudioTheme
{
    internal static void Apply(Application app)
    {
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, args) =>
        {
            if (sender is Window window && ReferenceEquals(args.OriginalSource, window) && window.Style is null)
                window.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
        }));
        app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse("""
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <SolidColorBrush x:Key="StudioBackground" Color="#11151D"/>
          <SolidColorBrush x:Key="StudioSurface" Color="#1C2330"/>
          <SolidColorBrush x:Key="StudioAccent" Color="#39C8BA"/>
          <Style TargetType="Window">
            <Setter Property="Background" Value="{StaticResource StudioBackground}"/>
            <Setter Property="Foreground" Value="#EDF2FA"/>
            <Setter Property="FontFamily" Value="Segoe UI"/>
            <Setter Property="FontSize" Value="14"/>
          </Style>
          <Style TargetType="TextBlock"><Setter Property="Foreground" Value="#EDF2FA"/></Style>
          <Style TargetType="Button">
            <Setter Property="Background" Value="#293548"/><Setter Property="Foreground" Value="#EDF2FA"/>
            <Setter Property="BorderBrush" Value="#40516A"/><Setter Property="BorderThickness" Value="1"/>
            <Setter Property="MinHeight" Value="44"/><Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button">
              <Border x:Name="Surface" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="8" Padding="{TemplateBinding Padding}">
                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" RecognizesAccessKey="True"/>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="#354962"/></Trigger>
                <Trigger Property="IsPressed" Value="True"><Setter TargetName="Surface" Property="Background" Value="#236D70"/></Trigger>
                <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#39C8BA"/></Trigger>
                <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Surface" Property="Opacity" Value="0.4"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate></Setter.Value></Setter>
          </Style>
          <Style TargetType="TextBox"><Setter Property="Background" Value="#11151D"/><Setter Property="Foreground" Value="#EDF2FA"/><Setter Property="CaretBrush" Value="White"/><Setter Property="BorderBrush" Value="#40516A"/><Setter Property="Padding" Value="8"/></Style>
          <Style TargetType="ComboBox"><Setter Property="Foreground" Value="#11151D"/><Setter Property="MinHeight" Value="34"/></Style>
          <Style TargetType="CheckBox"><Setter Property="Foreground" Value="#EDF2FA"/></Style>
          <Style TargetType="TabControl"><Setter Property="Background" Value="{StaticResource StudioBackground}"/><Setter Property="BorderThickness" Value="0"/><Setter Property="Padding" Value="0,16,0,0"/></Style>
          <Style TargetType="TabItem">
            <Setter Property="Foreground" Value="#AAB8CC"/><Setter Property="Padding" Value="18,12"/>
            <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="TabItem">
              <Border x:Name="Tab" Background="#1C2330" CornerRadius="8,8,0,0" Margin="0,0,6,0" Padding="{TemplateBinding Padding}"><ContentPresenter ContentSource="Header"/></Border>
              <ControlTemplate.Triggers><Trigger Property="IsSelected" Value="True"><Setter Property="Foreground" Value="#39C8BA"/><Setter TargetName="Tab" Property="Background" Value="#293548"/></Trigger><Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Tab" Property="BorderBrush" Value="#39C8BA"/><Setter TargetName="Tab" Property="BorderThickness" Value="1"/></Trigger></ControlTemplate.Triggers>
            </ControlTemplate></Setter.Value></Setter>
          </Style>
        </ResourceDictionary>
        """));
    }
}
