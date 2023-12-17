using GHelper.Helpers;
using System.Runtime.InteropServices;
using static GHelper.Gpu.AMD.Adl2.NativeMethods;

namespace GHelper.Gpu.AMD;

// Reference: https://github.com/GPUOpen-LibrariesAndSDKs/display-library/blob/master/Sample-Managed/Program.cs
public class AmdGpuControl : IGpuControl
{
    private bool _isReady;
    private nint _adlContextHandle;
    private readonly ADLAdapterInfo _internalDiscreteAdapter;

    public bool IsNvidia => false;

    public string FullName => _internalDiscreteAdapter!.AdapterName;

    private ADLAdapterInfo? FindByType(ADLAsicFamilyType type = ADLAsicFamilyType.Discrete)
    {
        ADL2_Adapter_NumberOfAdapters_Get(adlContextHandle: _adlContextHandle, numAdapters: out int numberOfAdapters);
        if (numberOfAdapters <= 0)
            return null;

        ADLAdapterInfoArray osAdapterInfoData = new();
        int osAdapterInfoDataSize = Marshal.SizeOf(structure: osAdapterInfoData);
        nint AdapterBuffer = Marshal.AllocCoTaskMem(cb: osAdapterInfoDataSize);
        Marshal.StructureToPtr(structure: osAdapterInfoData, ptr: AdapterBuffer, fDeleteOld: false);
        if (ADL2_Adapter_AdapterInfo_Get(adlContextHandle: _adlContextHandle, info: AdapterBuffer, inputSize: osAdapterInfoDataSize) != Adl2.ADL_SUCCESS)
            return null;

        osAdapterInfoData = (ADLAdapterInfoArray)Marshal.PtrToStructure(ptr: AdapterBuffer, structureType: osAdapterInfoData.GetType())!;

        const int amdVendorId = 1002;

        // Determine which GPU is internal discrete AMD GPU
        ADLAdapterInfo internalDiscreteAdapter =
            osAdapterInfoData.ADLAdapterInfo
                .FirstOrDefault(predicate: adapter =>
                {
                    if (adapter.Exist == 0 || adapter.Present == 0)
                        return false;

                    if (adapter.VendorID != amdVendorId)
                        return false;

                    if (ADL2_Adapter_ASICFamilyType_Get(adlContextHandle: _adlContextHandle, adapterIndex: adapter.AdapterIndex, asicFamilyType: out ADLAsicFamilyType asicFamilyType, asicFamilyTypeValids: out int asicFamilyTypeValids) != Adl2.ADL_SUCCESS)
                        return false;

                    asicFamilyType = (ADLAsicFamilyType)((int)asicFamilyType & asicFamilyTypeValids);

                    return (asicFamilyType & type) != 0;
                });

        if (internalDiscreteAdapter.Exist == 0)
            return null;

        return internalDiscreteAdapter;

    }

    public AmdGpuControl()
    {
        if (!Adl2.Load())
            return;

        if (Adl2.ADL2_Main_Control_Create(enumConnectedAdapters: 1, adlContextHandle: out _adlContextHandle) != Adl2.ADL_SUCCESS)
            return;

        ADLAdapterInfo? internalDiscreteAdapter = FindByType(type: ADLAsicFamilyType.Discrete);

        if (internalDiscreteAdapter is not null)
        {
            _internalDiscreteAdapter = (ADLAdapterInfo)internalDiscreteAdapter;
            _isReady = true;
        }

    }

    public bool IsValid => _isReady && _adlContextHandle != nint.Zero;

    public int? GetCurrentTemperature()
    {
        if (!IsValid)
            return null;

        if (ADL2_New_QueryPMLogData_Get(adlContextHandle: _adlContextHandle, adapterIndex: _internalDiscreteAdapter.AdapterIndex, adlpmLogDataOutput: out ADLPMLogDataOutput adlpmLogDataOutput) != Adl2.ADL_SUCCESS)
            return null;

        ADLSingleSensorData temperatureSensor = adlpmLogDataOutput.Sensors[(int)ADLSensorType.PMLOG_TEMPERATURE_EDGE];
        if (temperatureSensor.Supported == 0)
            return null;

        return temperatureSensor.Value;
    }


    public int? GetGpuUse()
    {
        if (!IsValid) return null;

        if (ADL2_New_QueryPMLogData_Get(adlContextHandle: _adlContextHandle, adapterIndex: _internalDiscreteAdapter.AdapterIndex, adlpmLogDataOutput: out ADLPMLogDataOutput adlpmLogDataOutput) != Adl2.ADL_SUCCESS)
            return null;

        ADLSingleSensorData gpuUsage = adlpmLogDataOutput.Sensors[(int)ADLSensorType.PMLOG_INFO_ACTIVITY_GFX];
        if (gpuUsage.Supported == 0)
            return null;

        return gpuUsage.Value;

    }


    public bool SetVariBright(int enabled)
    {
        if (_adlContextHandle == nint.Zero) return false;

        ADLAdapterInfo? iGPU = FindByType(type: ADLAsicFamilyType.Integrated);
        if (iGPU is null) return false;

        return ADL2_Adapter_VariBrightEnable_Set(context: _adlContextHandle, iAdapterIndex: ((ADLAdapterInfo)iGPU).AdapterIndex, iEnabled: enabled) == Adl2.ADL_SUCCESS;

    }

    public bool GetVariBright(out int supported, out int enabled)
    {
        supported = enabled = -1;

        if (_adlContextHandle == nint.Zero) return false;

        ADLAdapterInfo? iGPU = FindByType(type: ADLAsicFamilyType.Integrated);
        if (iGPU is null) return false;

        if (ADL2_Adapter_VariBright_Caps(context: _adlContextHandle, iAdapterIndex: ((ADLAdapterInfo)iGPU).AdapterIndex, iSupported: out int supportedOut, iEnabled: out int enabledOut, iVersion: out int version) != Adl2.ADL_SUCCESS)
            return false;

        supported = supportedOut;
        enabled = enabledOut;

        return true;
    }

    public ADLODNPerformanceLevels? GetGPUClocks()
    {
        if (!IsValid) return null;

        ADLODNPerformanceLevels performanceLevels = new();
        ADL2_OverdriveN_SystemClocks_Get(context: _adlContextHandle, adapterIndex: _internalDiscreteAdapter.AdapterIndex, performanceLevels: ref performanceLevels);

        return performanceLevels;
    }

    public void KillGPUApps()
    {

        if (!IsValid) return;

        nint appInfoPtr = nint.Zero;
        int appCount = 0;

        try
        {
            // Get switchable graphics applications information
            var result = ADL2_SwitchableGraphics_Applications_Get(context: _adlContextHandle, iListType: 2, lpNumApps: out appCount, lppAppList: out appInfoPtr);
            if (result != 0)
            {
                throw new Exception(message: "Failed to get switchable graphics applications. Error code: " + result);
            }

            // Convert the application data pointers to an array of structs
            var appInfoArray = new ADLSGApplicationInfo[appCount];
            nint currentPtr = appInfoPtr;

            for (int i = 0; i < appCount; i++)
            {
                appInfoArray[i] = Marshal.PtrToStructure<ADLSGApplicationInfo>(ptr: currentPtr);
                currentPtr = nint.Add(pointer: currentPtr, offset: Marshal.SizeOf<ADLSGApplicationInfo>());
            }

            var appNames = new List<string>();

            for (int i = 0; i < appCount; i++)
            {
                if (appInfoArray[i].iGPUAffinity == 1)
                {
                    Logger.WriteLine(logMessage: appInfoArray[i].strFileName + ":" + appInfoArray[i].iGPUAffinity + "(" + appInfoArray[i].timeStamp + ")");
                    appNames.Add(item: Path.GetFileNameWithoutExtension(path: appInfoArray[i].strFileName));
                }
            }

            List<string> immune = new() { "svchost", "system", "ntoskrnl", "csrss", "winlogon", "wininit", "smss" };

            foreach (string kill in appNames)
                if (!immune.Contains(item: kill.ToLower()))
                    ProcessHelper.KillByName(name: kill);


        }
        catch (Exception ex)
        {
            Logger.WriteLine(logMessage: ex.Message);
        }
        finally
        {
            // Clean up resources
            if (appInfoPtr != nint.Zero)
            {
                Marshal.FreeCoTaskMem(ptr: appInfoPtr);
            }

        }
    }


    private void ReleaseUnmanagedResources()
    {
        if (_adlContextHandle != nint.Zero)
        {
            ADL2_Main_Control_Destroy(adlContextHandle: _adlContextHandle);
            _adlContextHandle = nint.Zero;
            _isReady = false;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(obj: this);
    }

    ~AmdGpuControl()
    {
        ReleaseUnmanagedResources();
    }
}
