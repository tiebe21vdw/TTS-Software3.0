using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Security;

public static class AutoStartManager
{
    private const string AppName = "TTS-Software";
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool SetAutoStart(bool enable)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            if (key is null) return false;

            if (enable)
            {
                var startupCommand = GetStartupCommand();
                if (startupCommand is null) return false;

                key.SetValue(AppName, startupCommand);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) is not null;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    private static string? GetStartupCommand()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath)) return null;

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var assemblyPath = Assembly.GetEntryAssembly()?.Location;
            return string.IsNullOrWhiteSpace(assemblyPath)
                ? null
                : $"\"{processPath}\" \"{assemblyPath}\"";
        }

        return $"\"{processPath}\"";
    }
}