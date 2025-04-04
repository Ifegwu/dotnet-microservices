# dotnet-microservices
Building Microservices with .NET
---
Process to locally build NuGet package `Play.Common` and add it as dependencies to other microservices:

### **1. inside /Play.Common/src/Play.Common 
dotnet pack -o ./nupkgs

### **2. inside /Play.Common/src/Play.Common --> targeting /dotnet-microservices/packages directory
dotnet pack -o ../../packages/ 

### **3. Add the locally built package to the nuget list
dotnet add package Play.Common --source /home/USER/workspace/PlayEconomy/packages/

### **4. list nuget packages 
dotnet nuget list source 

### **5. Add Play.Common package as a dependency a repository
dotnet nuget add source /home/USER/dotnet-microservices/packages -n Play.Common 
---
---
To version your locally built NuGet package incrementally, follow these steps:

### **1. Update the Version Number**
Before running `dotnet pack`, update the version in your `.csproj` file. Locate the following line and increment the version (e.g., `1.0.0` → `1.0.1`):

```xml
<PropertyGroup>
    <Version>1.0.1</Version>
</PropertyGroup>
```

Alternatively, you can specify the version dynamically when running `dotnet pack`:

```sh
dotnet pack -o ../../packages/ -p:Version=1.0.1
```

### **2. Build and Pack the NuGet Package**
Run:

```sh
dotnet pack -o ../../packages/ -p:Version=1.0.1
```

This ensures the new version is correctly built and stored in `../../packages/`.

### **3. Remove the Old Package (Optional)**
If you need to remove an old version from your local NuGet source:

```sh
rm /home/USER/dotnet-microservices/packages/Play.Common.*.nupkg
```

### **4. Add the New Version as a Dependency**
Since you've already added the source, you only need to add the updated package:

```sh
dotnet add package Play.Common --version 1.0.1 --source /home/USER/dotnet-microservices/packages/
```

### **5. Verify the Installed Version**
Check if the correct version is installed:

```sh
dotnet list package
```

This process ensures that your locally built NuGet package is versioned and correctly linked to your project without conflicts.
---