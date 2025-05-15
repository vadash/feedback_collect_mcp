# Clear the terminal first
Clear-Host

# --- NO Read-Host here if the WPF GUI is for the primary input ---

# --- START WPF CODE DIRECTLY ---
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

[xml]$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Enter Multi-line Text (WPF)" Height="300" Width="450"
        WindowStartupLocation="CenterScreen"
        ResizeMode="CanResizeWithGrip">
    <Grid Margin="15">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Label Grid.Row="0" Content="Please enter your text (multi-line allowed):" Margin="0,0,0,5" FontWeight="SemiBold"/>
        <TextBox x:Name="InputTextBox"
                 Grid.Row="1" Margin="0,0,0,10" Padding="5"
                 AcceptsReturn="True" TextWrapping="Wrap"
                 VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto"
                 SpellCheck.IsEnabled="True" MinHeight="100"/>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button x:Name="OkButton" Content="OK" Width="80" Height="30" Margin="0,0,10,0" IsDefault="True"/>
            <Button x:Name="CancelButton" Content="Cancel" Width="80" Height="30" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
"@

$reader = (New-Object System.Xml.XmlNodeReader $xaml)
try {
    $window = [Windows.Markup.XamlReader]::Load( $reader )
} catch {
    Write-Error "Error loading XAML: $($_.Exception.Message)"
    exit 1
}

$textBox = $window.FindName("InputTextBox")
$okButton = $window.FindName("OkButton")
$cancelButton = $window.FindName("CancelButton")

$script:userInput = $null
$script:dialogResult = $false

$okButton.Add_Click({
    $script:userInput = $textBox.Text
    $script:dialogResult = $true
    $window.Close()
})

$cancelButton.Add_Click({
    $script:dialogResult = $false
    $window.Close()
})

$window.Add_SourceInitialized({ $textBox.Focus() | Out-Null })

$null = $window.ShowDialog()

if ($script:dialogResult -eq $true) {
    Write-Host "You entered via GUI:"
    Write-Host "------------"
    Write-Host $script:userInput
    Write-Host "------------"
} else {
    Write-Host "User cancelled or closed the GUI dialog."
}