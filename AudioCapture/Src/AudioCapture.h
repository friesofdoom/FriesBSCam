#pragma once
#include "common.h"
#include <comutil.h>

namespace AudioCapture
{
    extern "C"
    {
        __declspec(dllexport) int Init(BSTR deviceName);
        __declspec(dllexport) int Shutdown();
        __declspec(dllexport) int GetNumChannels();
        __declspec(dllexport) bool HasError();
        __declspec(dllexport) void ReadData(byte* data, int numBytes);
        __declspec(dllexport) BSTR GetSelectedDeviceName();
        __declspec(dllexport) BSTR GetDeviceName(int index);
        __declspec(dllexport) int GetNumDevices();
    };


    DWORD WINAPI LoopbackCaptureThreadFunction(LPVOID pContext);
    HRESULT LoopbackCapture(
        IMMDevice* pMMDevice,
        bool bInt16,
        HANDLE hStartedEvent,
        PUINT32 pnFrames
    );
};