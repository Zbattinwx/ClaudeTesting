# Running Ohio Weather in Visual Studio 2022

## Quick Start Guide

### Prerequisites
- Visual Studio 2022 (Community, Professional, or Enterprise)
- .NET 8 SDK installed
- Workload: ".NET Desktop Development" (includes WPF)

### Step 1: Restore NuGet Packages

The error `Assets file 'obj\project.assets.json' not found` means NuGet packages haven't been restored yet.

**Option A: Using Visual Studio UI**
1. Open `OhioWeather.csproj` or the solution in Visual Studio 2022
2. Right-click on the project in Solution Explorer
3. Select **"Restore NuGet Packages"**
4. Wait for the restore to complete (check Output window)

**Option B: Using Package Manager Console**
1. Go to **Tools > NuGet Package Manager > Package Manager Console**
2. Run:
   ```
   dotnet restore
   ```

**Option C: Using Command Line**
1. Open Command Prompt or PowerShell
2. Navigate to the project directory:
   ```
   cd C:\Users\zbattin\Documents\ClaudeTesting\ClaudeTesting
   ```
3. Run:
   ```
   dotnet restore
   ```

### Step 2: Clean and Rebuild

After restoring packages:

1. In Visual Studio, go to **Build > Clean Solution**
2. Then go to **Build > Rebuild Solution**
3. Check the Output window for any errors

### Step 3: Verify .NET 8 SDK Installation

If you still have issues, verify .NET 8 is installed:

**Command Line:**
```
dotnet --list-sdks
```

You should see version 8.0.x listed. If not:
- Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Install ".NET 8.0 SDK (v8.x.x)"

### Step 4: Fix XAML Intellisense Issues

The "Padding property does not exist" error is likely an IntelliSense cache issue:

1. Close Visual Studio
2. Delete these folders if they exist:
   - `bin\`
   - `obj\`
   - `.vs\` (hidden folder)
3. Reopen Visual Studio
4. Restore NuGet packages again
5. Rebuild solution

### Step 5: Run the Application

1. Make sure `OhioWeather` is set as the startup project (bold in Solution Explorer)
2. Press **F5** or click the **Start** button (green play icon)
3. The app should launch with the main window

## Common Issues and Solutions

### Issue: "Cannot find MaterialDesignThemes"
**Solution:** Ensure these packages are installed:
- MaterialDesignThemes (v4.9.0)
- MaterialDesignThemes.Wpf
- CommunityToolkit.Mvvm (v8.2.2)

Run in Package Manager Console:
```
Install-Package MaterialDesignThemes -Version 4.9.0
Install-Package CommunityToolkit.Mvvm -Version 8.2.2
```

### Issue: "Target framework 'net8.0-windows' not found"
**Solution:**
1. Install .NET 8 SDK from Microsoft
2. Restart Visual Studio
3. Rebuild the project

### Issue: XAML designer not loading
**Solution:**
- The XAML designer can be slow or fail to load for WPF projects
- You can still run the app even if the designer doesn't work
- Consider using the live preview feature: **Debug > Hot Reload > Hot Reload on File Save**

### Issue: "App.xaml.cs has errors"
**Solution:**
Make sure all service classes are found. The error might be IntelliSense lag. Try:
1. Build the solution (**Ctrl+Shift+B**)
2. If it builds successfully, the red squiggles are false positives
3. Running the app will work even with IntelliSense errors

## Project Structure Verification

Your project folder should look like this:
```
ClaudeTesting/
├── Assets/
│   ├── ONW_Logo.png
│   ├── upper_third_cc.html
│   ├── upper_third_reflectivity.html
│   └── upper_third_velocity.html
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── OhioWeather.csproj
└── README.md
```

## Expected First Run

When you run the app:
1. Main window opens with Ohio News and Weather branding
2. Sidebar navigation on the left
3. Click **"Radar"** button to load the radar view
4. Select a radar site (KCLE - Cleveland is default)
5. Click **"Load Radar"** to fetch radar data

**Note:** The first radar load might take a few seconds as it downloads imagery from NOAA.

## Building for Release

To create a standalone executable:

1. Right-click project > **Publish**
2. Choose **Folder** as target
3. Select configuration: **Release**
4. Click **Publish**

Or use command line:
```
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in: `bin\Release\net8.0-windows\win-x64\publish\`

## Troubleshooting Checklist

- [ ] .NET 8 SDK installed
- [ ] NuGet packages restored
- [ ] bin/ and obj/ folders deleted and recreated
- [ ] Solution built successfully (no build errors)
- [ ] Visual Studio restarted
- [ ] Windows Firewall allows internet access (for radar data)

## Still Having Issues?

If you're still getting errors after following these steps:

1. **Share the exact error message** from the Error List window
2. **Check the Output window** (View > Output) and select "Build" from dropdown
3. Try running from command line:
   ```
   cd C:\Users\zbattin\Documents\ClaudeTesting\ClaudeTesting
   dotnet build
   dotnet run
   ```
4. Share any error messages from the command line

Let me know which step you're stuck on and I'll help you resolve it!
