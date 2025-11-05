# Quick Start - Visual Studio 2022

## The Fast Way (Recommended)

### Step 1: Run the Setup Script
1. Navigate to the project folder in File Explorer
2. Double-click **`restore-packages.bat`**
3. Wait for it to complete (should say "SUCCESS!")

### Step 2: Open in Visual Studio
1. Double-click **`OhioWeather.sln`**
2. Visual Studio 2022 will open
3. Wait for IntelliSense to finish loading (check bottom-left status bar)

### Step 3: Run the App
1. Press **F5** (or click the green ▶ Start button)
2. The Ohio Weather app will launch!

---

## If You Get Errors

### Error: "project.assets.json not found"

**In Visual Studio:**
1. Right-click the project in Solution Explorer
2. Select **"Restore NuGet Packages"**
3. Wait for completion
4. **Build > Rebuild Solution**

**Or via Command Line:**
```cmd
cd C:\Users\zbattin\Documents\ClaudeTesting\ClaudeTesting
dotnet restore
dotnet build
```

### Error: "Padding property does not exist"

This is a **false error** from IntelliSense. It will go away after:
1. NuGet packages are restored
2. Project is built successfully
3. Visual Studio is restarted

**To fix immediately:**
1. Close Visual Studio
2. Delete `bin`, `obj`, and `.vs` folders
3. Run `restore-packages.bat`
4. Reopen `OhioWeather.sln`

### Error: ".NET 8 SDK not found"

**Install .NET 8:**
1. Go to: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download ".NET 8.0 SDK (recommended)"
3. Run the installer
4. Restart Visual Studio
5. Try again

---

## Verify Installation

Open Command Prompt and run:
```cmd
dotnet --list-sdks
```

You should see:
```
8.0.xxx [C:\Program Files\dotnet\sdk]
```

---

## First Run

When the app launches:
1. You'll see the main window with sidebar navigation
2. Click **"Radar"** in the sidebar
3. Select a radar site (KCLE - Cleveland is default)
4. Click **"Load Radar"**
5. Wait a few seconds for radar imagery to download
6. Click **Play** to animate the radar loop!

---

## Need Help?

If you're still stuck, check the full guide in **SETUP_GUIDE.md**

Or share your exact error message and I'll help you fix it!
