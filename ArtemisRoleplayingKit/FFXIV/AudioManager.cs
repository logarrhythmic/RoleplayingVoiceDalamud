﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal class AudioManager {
    public static void ToggleProcessMute(Process process) {
        var mute = VolumeMixer.GetApplicationMute(process.Id);
        VolumeMixer.SetApplicationMute(process.Id, !mute);
    }
    public static void SetProcessMute(Process process, bool mute) {
        VolumeMixer.SetApplicationMute(process.Id, mute);
    }
    public static bool GetProcessMute(Process process) {
        return VolumeMixer.GetApplicationMute(process.Id);
    }
}

internal class VolumeMixer {
    public static float GetApplicationVolume(int pid) {
        var volume = GetVolumeObject(pid);
        if (volume == null)
            return 0.0f;

        volume.GetMasterVolume(out var level);
        Marshal.ReleaseComObject(volume);
        return level * 100;
    }

    public static bool GetApplicationMute(int pid) {
        var volume = GetVolumeObject(pid);
        if (volume == null)
            return false;

        volume.GetMute(out var mute);
        Marshal.ReleaseComObject(volume);
        return mute;
    }

    public static void SetApplicationVolume(int pid, float level) {
        var volume = GetVolumeObject(pid);
        if (volume == null)
            return;


        var guid = Guid.Empty;
        volume.SetMasterVolume(level / 100, ref guid);
        Marshal.ReleaseComObject(volume);
    }

    public static void SetApplicationMute(int pid, bool mute) {
        var volume = GetVolumeObject(pid);
        if (volume == null) {
            return;
        }

        var guid = Guid.Empty;
        volume.SetMute(mute, ref guid);
        Marshal.ReleaseComObject(volume);
    }

    private static ISimpleAudioVolume GetVolumeObject(int pid) {
        // get the speakers (1st render + multimedia) device
        try {
            var deviceEnumerator = (IMmDeviceEnumerator)new MMDeviceEnumerator();
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.ERender, ERole.EMultimedia, out var speakers);

            // activate the session manager. we need the enumerator
            var iidIAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            speakers.Activate(ref iidIAudioSessionManager2, 0, IntPtr.Zero, out var o);
            var mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            mgr.GetSessionEnumerator(out var sessionEnumerator);
            sessionEnumerator.GetCount(out var count);

            // search for an audio session with the required name
            // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ISimpleAudioVolume volumeControl = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            for (var i = 0; i < count; i++) {
                sessionEnumerator.GetSession(i, out var ctl);
                ctl.GetProcessId(out var cpid);

                if (cpid == pid) {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    volumeControl = ctl as ISimpleAudioVolume;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    break;
                }

                Marshal.ReleaseComObject(ctl);
            }

            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
#pragma warning disable CS8603 // Possible null reference return.
            return volumeControl;
#pragma warning restore CS8603 // Possible null reference return.
        } catch {
#pragma warning disable CS8603 // Possible null reference return.
            return null;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumerator {
}

internal enum EDataFlow {
    ERender,
    ECapture,
    EAll,
    EDataFlowEnumCount
}

internal enum ERole {
    EConsole,
    EMultimedia,
    ECommunications,
    ERoleEnumCount
}

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMmDeviceEnumerator {
    int NotImpl1();

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMmDevice ppDevice);

    // the rest is not implemented
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMmDevice {
    [PreserveSig]
    int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

    // the rest is not implemented
}

[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionManager2 {
    int NotImpl1();

    int NotImpl2();

    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);

    // the rest is not implemented
}

[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEnumerator {
    [PreserveSig]
    int GetCount(out int sessionCount);

    [PreserveSig]
    int GetSession(int sessionCount, out IAudioSessionControl2 session);
}

[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISimpleAudioVolume {
    [PreserveSig]
    int SetMasterVolume(float fLevel, ref Guid eventContext);

    [PreserveSig]
    int GetMasterVolume(out float pfLevel);

    [PreserveSig]
    int SetMute(bool bMute, ref Guid eventContext);

    [PreserveSig]
    int GetMute(out bool pbMute);
}

[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl2 {
    // IAudioSessionControl
    [PreserveSig]
    int NotImpl0();

    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value,
        [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

    [PreserveSig]
    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value,
        [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

    [PreserveSig]
    int GetGroupingParam(out Guid pRetVal);

    [PreserveSig]
    int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid @override,
        [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

    [PreserveSig]
    int NotImpl1();

    [PreserveSig]
    int NotImpl2();

    // IAudioSessionControl2
    [PreserveSig]
    int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int GetProcessId(out int pRetVal);

    [PreserveSig]
    int IsSystemSoundsSession();

    [PreserveSig]
    int SetDuckingPreference(bool optOut);
}