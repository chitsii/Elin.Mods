$extensions = @(".cs", ".xml", ".json", ".md", ".txt", ".csproj", ".sln")
$path = "c:\Users\tishi\programming\elin_modding\ElinMod\Elin_ItemRelocator"

Get-ChildItem -Path $path -Recurse | Where-Object {
    $ext = $_.Extension
    $extensions -contains $ext
} | ForEach-Object {
    try {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        if ($content.Contains("`r`n")) {
            $content = $content.Replace("`r`n", "`n")
            [System.IO.File]::WriteAllText($_.FullName, $content)
            Write-Host "Converted: $($_.FullName)"
        }
    } catch {
        Write-Host "Error processing $($_.FullName): $_"
    }
}
Write-Host "Done."
