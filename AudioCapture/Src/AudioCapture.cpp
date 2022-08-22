#include "AudioCapture.h"
#include "Helpers.h"
#include "common.h"
#include <chrono>
#include <thread>

namespace AudioCapture
{
    struct LoopbackCaptureThreadFunctionArguments {
        IMMDevice* pMMDevice;
        bool bInt16;
        HANDLE hStartedEvent;
        UINT32 nFrames;
        HRESULT hr;
    };

    std::vector<std::wstring> globalDeviceList;
    IMMDeviceEnumerator* globalMMDeviceEnumerator;
    HANDLE globalHThread;
    HANDLE globalHStartedEvent;
    LoopbackCaptureThreadFunctionArguments globalThreadArgs;
    WAVEFORMATEX* globalPwfx;
    bool globalErrorCondition = false;
    bool globalDone = false;
    bool globalFinished = false;
    int globalSleepTime = 100;

    const int globalBufferSize = 1 * 1024 * 1024; // Must be power of 2
    int globalCurrentWritePos = 0;
    byte globalBigBuffer[globalBufferSize];

    class MMNotificationClient : public IMMNotificationClient
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnDeviceStateChanged(
            _In_  LPCWSTR pwstrDeviceId,
            _In_  DWORD dwNewState)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
            globalErrorCondition = true;
            return S_OK;
        }

        virtual HRESULT STDMETHODCALLTYPE OnDeviceAdded(
            _In_  LPCWSTR pwstrDeviceId)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime)); 
            globalErrorCondition = true;
            return S_OK;
        }

        virtual HRESULT STDMETHODCALLTYPE OnDeviceRemoved(
            _In_  LPCWSTR pwstrDeviceId)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
            globalErrorCondition = true;
            return S_OK;
        }

        virtual HRESULT STDMETHODCALLTYPE OnDefaultDeviceChanged(
            _In_  EDataFlow flow,
            _In_  ERole role,
            _In_  LPCWSTR pwstrDefaultDeviceId)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime)); 
            globalErrorCondition = true;
            return S_OK;
        }

        virtual HRESULT STDMETHODCALLTYPE OnPropertyValueChanged(
            _In_  LPCWSTR pwstrDeviceId,
            _In_  const PROPERTYKEY key)
        {
            return S_OK;
        }

        virtual ULONG STDMETHODCALLTYPE AddRef(void) { return 1; }
        virtual ULONG STDMETHODCALLTYPE Release(void) { return 1; }
        IFACEMETHODIMP QueryInterface(REFIID iid, void** object) override
        {
            if (iid == IID_IUnknown || iid == __uuidof(IMMNotificationClient))
            {
                *object = static_cast<IMMNotificationClient*>(this);
                return S_OK;
            }
            *object = nullptr;
            return E_NOINTERFACE;
        };
    };

    MMNotificationClient globalMMNotificationClient;


    HRESULT get_default_device(IMMDevice** ppMMDevice) {
        HRESULT hr = S_OK;

        // get the default render endpoint
        hr = globalMMDeviceEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, ppMMDevice);
        if (FAILED(hr)) {
            ERR(L"IMMDeviceEnumerator::GetDefaultAudioEndpoint failed: hr = 0x%08x", hr);
            return hr;
        }

        return S_OK;
    }

    HRESULT list_devices() {
        HRESULT hr = S_OK;

        IMMDeviceCollection* pMMDeviceCollection;

        // get all the active render endpoints
        hr = globalMMDeviceEnumerator->EnumAudioEndpoints(
            eAll, DEVICE_STATE_ACTIVE, &pMMDeviceCollection
        );
        if (FAILED(hr)) {
            ERR(L"IMMDeviceEnumerator::EnumAudioEndpoints failed: hr = 0x%08x", hr);
            return hr;
        }
        ReleaseOnExit releaseMMDeviceCollection(pMMDeviceCollection);

        UINT count;
        hr = pMMDeviceCollection->GetCount(&count);
        if (FAILED(hr)) {
            ERR(L"IMMDeviceCollection::GetCount failed: hr = 0x%08x", hr);
            return hr;
        }
        LOG(L"Active render endpoints found: %u", count);

        for (UINT i = 0; i < count; i++) {
            IMMDevice* pMMDevice;

            // get the "n"th device
            hr = pMMDeviceCollection->Item(i, &pMMDevice);
            if (FAILED(hr)) {
                ERR(L"IMMDeviceCollection::Item failed: hr = 0x%08x", hr);
                return hr;
            }
            ReleaseOnExit releaseMMDevice(pMMDevice);

            // open the property store on that device
            IPropertyStore* pPropertyStore;
            hr = pMMDevice->OpenPropertyStore(STGM_READ, &pPropertyStore);
            if (FAILED(hr)) {
                ERR(L"IMMDevice::OpenPropertyStore failed: hr = 0x%08x", hr);
                return hr;
            }
            ReleaseOnExit releasePropertyStore(pPropertyStore);

            // get the long name property
            PROPVARIANT pv; PropVariantInit(&pv);
            hr = pPropertyStore->GetValue(PKEY_Device_FriendlyName, &pv);
            if (FAILED(hr)) {
                ERR(L"IPropertyStore::GetValue failed: hr = 0x%08x", hr);
                return hr;
            }
            PropVariantClearOnExit clearPv(&pv);

            if (VT_LPWSTR != pv.vt) {
                ERR(L"PKEY_Device_FriendlyName variant type is %u - expected VT_LPWSTR", pv.vt);
                return E_UNEXPECTED;
            }

            LOG(L"    %ls", pv.pwszVal);
            globalDeviceList.push_back(pv.pwszVal);
        }

        return S_OK;
    }

    HRESULT get_specific_device(LPCWSTR szLongName, IMMDevice** ppMMDevice) {
        HRESULT hr = S_OK;

        *ppMMDevice = NULL;

        // get an enumerator
        
        IMMDeviceCollection* pMMDeviceCollection;

        // get all the active render endpoints
        hr = globalMMDeviceEnumerator->EnumAudioEndpoints(
            eAll, DEVICE_STATE_ACTIVE, &pMMDeviceCollection
        );
        if (FAILED(hr)) {
            ERR(L"IMMDeviceEnumerator::EnumAudioEndpoints failed: hr = 0x%08x", hr);
            return hr;
        }
        ReleaseOnExit releaseMMDeviceCollection(pMMDeviceCollection);

        UINT count;
        hr = pMMDeviceCollection->GetCount(&count);
        if (FAILED(hr)) {
            ERR(L"IMMDeviceCollection::GetCount failed: hr = 0x%08x", hr);
            return hr;
        }

        for (UINT i = 0; i < count; i++) {
            IMMDevice* pMMDevice;

            // get the "n"th device
            hr = pMMDeviceCollection->Item(i, &pMMDevice);
            if (FAILED(hr)) {
                ERR(L"IMMDeviceCollection::Item failed: hr = 0x%08x", hr);
                return hr;
            }
            ReleaseOnExit releaseMMDevice(pMMDevice);

            // open the property store on that device
            IPropertyStore* pPropertyStore;
            hr = pMMDevice->OpenPropertyStore(STGM_READ, &pPropertyStore);
            if (FAILED(hr)) {
                ERR(L"IMMDevice::OpenPropertyStore failed: hr = 0x%08x", hr);
                return hr;
            }
            ReleaseOnExit releasePropertyStore(pPropertyStore);

            // get the long name property
            PROPVARIANT pv; PropVariantInit(&pv);
            hr = pPropertyStore->GetValue(PKEY_Device_FriendlyName, &pv);
            if (FAILED(hr)) {
                ERR(L"IPropertyStore::GetValue failed: hr = 0x%08x", hr);
                return hr;
            }
            PropVariantClearOnExit clearPv(&pv);

            if (VT_LPWSTR != pv.vt) {
                ERR(L"PKEY_Device_FriendlyName variant type is %u - expected VT_LPWSTR", pv.vt);
                return E_UNEXPECTED;
            }

            // is it a match?
            if (0 == _wcsicmp(pv.pwszVal, szLongName)) {
                // did we already find it?
                if (NULL == *ppMMDevice) {
                    *ppMMDevice = pMMDevice;
                    pMMDevice->AddRef();
                }
                else {
                    ERR(L"Found (at least) two devices named %ls", szLongName);
                    return E_UNEXPECTED;
                }
            }
        }

        if (NULL == *ppMMDevice) {
            ERR(L"Could not find a device named %ls", szLongName);
            return HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        }

        return S_OK;
    }


    void WriteBuffer(byte* bytes, int numBytes)
    {
        for (int i = 0; i < numBytes; i++)
        {
            int writePos = (globalCurrentWritePos + i) & (globalBufferSize - 1);
            globalBigBuffer[writePos] = bytes[i];
        }
        globalCurrentWritePos = (globalCurrentWritePos + numBytes) & (globalBufferSize - 1);
    }

    void ReadData(byte* data, int numBytes)
    {
        int startPos = (globalCurrentWritePos + globalBufferSize - numBytes) & (globalBufferSize - 1);
        for (int i = 0; i < numBytes; i++)
        {
            int readPos = (startPos + i) & (globalBufferSize - 1);
            data[i] = globalBigBuffer[readPos];
        }
    }

    BSTR ANSItoBSTR(char* input)
    {
        BSTR result = NULL;
        int lenA = lstrlenA(input);
        int lenW = ::MultiByteToWideChar(CP_ACP, 0, input, lenA, NULL, 0);
        if (lenW > 0)
        {
            result = ::SysAllocStringLen(0, lenW);
            ::MultiByteToWideChar(CP_ACP, 0, input, lenA, result, lenW);
        }
        return result;
    }

    BSTR GetDeviceName(int index)
    {
        return ::SysAllocString(globalDeviceList[index].c_str());
    }

    int GetNumDevices()
    {
        return (int)globalDeviceList.size();
    }

    BSTR GetSelectedDeviceName()
    {
        LPWSTR id;
        globalThreadArgs.pMMDevice->GetId(&id);
        return ::SysAllocString(id);
    }

    int Init(BSTR deviceName)
    {
        globalDone = false;
        globalFinished = false;
        globalErrorCondition = false;
        HRESULT hr = S_OK;
        hr = CoInitialize(NULL);
        if (FAILED(hr)) {
            ERR(L"CoInitialize failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return -__LINE__;
        }

        // create a "loopback capture has started" event
        globalHStartedEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        if (NULL == globalHStartedEvent) {
            ERR(L"CreateEvent failed: last error is %u", GetLastError());
            globalErrorCondition = true;
            globalFinished = true;
            return -__LINE__;
        }
        CloseHandleOnExit closeStartedEvent(globalHStartedEvent);

        hr = CoCreateInstance(
            __uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL,
            __uuidof(IMMDeviceEnumerator),
            (void**)&globalMMDeviceEnumerator
        );
        if (FAILED(hr)) {
            ERR(L"CoCreateInstance(IMMDeviceEnumerator) failed: hr = 0x%08x", hr);
            return hr;
        }

        // create arguments for loopback capture thread
        globalThreadArgs.hr = E_UNEXPECTED; // thread will overwrite this
        HRESULT res = get_specific_device(deviceName, &globalThreadArgs.pMMDevice);

        if (res != S_OK)
        {
            res = get_default_device(&globalThreadArgs.pMMDevice);
            if (res != S_OK)
            {
                globalErrorCondition = true;
                globalFinished = true;
                return -__LINE__;
            }
        }

        globalThreadArgs.bInt16 = true;
        globalThreadArgs.hStartedEvent = globalHStartedEvent;
        globalThreadArgs.nFrames = 0;

        globalHThread = CreateThread(
            NULL, 0,
            LoopbackCaptureThreadFunction, &globalThreadArgs,
            0, NULL
        );
        if (NULL == globalHThread) {
            ERR(L"CreateThread failed: last error is %u", GetLastError());
            globalErrorCondition = true;
            globalFinished = true;
            return -__LINE__;
        }

        globalMMDeviceEnumerator->RegisterEndpointNotificationCallback(&globalMMNotificationClient);
        
        list_devices();

        return 0;
    }

    int Shutdown()
    {
        globalDone = true;
        while (!globalFinished);

        globalDone = false;
        globalFinished = false;
        globalErrorCondition = false;

        globalMMDeviceEnumerator->UnregisterEndpointNotificationCallback(&globalMMNotificationClient);
        globalMMDeviceEnumerator->Release();

        DWORD exitCode;
        if (!GetExitCodeThread(globalHThread, &exitCode)) {
            ERR(L"GetExitCodeThread failed: last error is %u", GetLastError());
            return -__LINE__;
        }

        if (!CloseHandle(globalHThread)) {
            ERR(L"CloseHandle failed: last error is %d", GetLastError());
        }

        if (0 != exitCode) {
            ERR(L"Loopback capture thread exit code is %u; expected 0", exitCode);
            //return -__LINE__;
        }

        if (S_OK != globalThreadArgs.hr) {
            ERR(L"Thread HRESULT is 0x%08x", globalThreadArgs.hr);
            //return -__LINE__;
        }

        CoUninitialize();

        return 0;
    }

    int GetNumChannels()
    {
        return globalPwfx == nullptr ? 0 : globalPwfx->nChannels;
    }

    bool HasError()
    {
        return globalErrorCondition;
    }

    DWORD WINAPI LoopbackCaptureThreadFunction(LPVOID pContext) {
        LoopbackCaptureThreadFunctionArguments* pArgs =
            (LoopbackCaptureThreadFunctionArguments*)pContext;

        pArgs->hr = CoInitialize(NULL);
        if (FAILED(pArgs->hr)) {
            ERR(L"CoInitialize failed: hr = 0x%08x", pArgs->hr);
            globalErrorCondition = true;
            globalFinished = true;
            return 0;
        }
        CoUninitializeOnExit cuoe;

        pArgs->hr = LoopbackCapture(
            pArgs->pMMDevice,
            pArgs->bInt16,
            pArgs->hStartedEvent,
            &pArgs->nFrames
        );

        globalErrorCondition = true;
        globalFinished = true;
        return 0;
    }

    HRESULT LoopbackCapture(
        IMMDevice* pMMDevice,
        bool bInt16,
        HANDLE hStartedEvent,
        PUINT32 pnFrames
    ) {
        HRESULT hr;

        // activate an IAudioClient
        IAudioClient* pAudioClient;
        hr = pMMDevice->Activate(
            __uuidof(IAudioClient),
            CLSCTX_ALL, NULL,
            (void**)&pAudioClient
        );
        if (FAILED(hr)) {
            ERR(L"IMMDevice::Activate(IAudioClient) failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }
        ReleaseOnExit releaseAudioClient(pAudioClient);

        // get the default device periodicity
        REFERENCE_TIME hnsDefaultDevicePeriod;
        hr = pAudioClient->GetDevicePeriod(&hnsDefaultDevicePeriod, NULL);
        if (FAILED(hr)) {
            ERR(L"IAudioClient::GetDevicePeriod failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }

        // get the default device format
        hr = pAudioClient->GetMixFormat(&globalPwfx);
        if (FAILED(hr)) {
            ERR(L"IAudioClient::GetMixFormat failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }
        CoTaskMemFreeOnExit freeMixFormat(globalPwfx);

        if (bInt16) {
            // coerce int-16 wave format
            // can do this in-place since we're not changing the size of the format
            // also, the engine will auto-convert from float to int for us
            switch (globalPwfx->wFormatTag) {
            case WAVE_FORMAT_IEEE_FLOAT:
                globalPwfx->wFormatTag = WAVE_FORMAT_PCM;
                globalPwfx->wBitsPerSample = 16;
                globalPwfx->nBlockAlign = globalPwfx->nChannels * globalPwfx->wBitsPerSample / 8;
                globalPwfx->nAvgBytesPerSec = globalPwfx->nBlockAlign * globalPwfx->nSamplesPerSec;
                break;

            case WAVE_FORMAT_EXTENSIBLE:
            {
                // naked scope for case-local variable
                PWAVEFORMATEXTENSIBLE pEx = reinterpret_cast<PWAVEFORMATEXTENSIBLE>(globalPwfx);
                if (IsEqualGUID(KSDATAFORMAT_SUBTYPE_IEEE_FLOAT, pEx->SubFormat)) {
                    pEx->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
                    pEx->Samples.wValidBitsPerSample = 16;
                    globalPwfx->wBitsPerSample = 16;
                    globalPwfx->nBlockAlign = globalPwfx->nChannels * globalPwfx->wBitsPerSample / 8;
                    globalPwfx->nAvgBytesPerSec = globalPwfx->nBlockAlign * globalPwfx->nSamplesPerSec;
                }
                else {
                    ERR(L"%s", L"Don't know how to coerce mix format to int-16");
                    globalErrorCondition = true;
                    globalFinished = true;
                    return E_UNEXPECTED;
                }
            }
            break;

            default:
                ERR(L"Don't know how to coerce WAVEFORMATEX with wFormatTag = 0x%08x to int-16", globalPwfx->wFormatTag);
                globalErrorCondition = true;
                globalFinished = true;
                return E_UNEXPECTED;
            }
        }

        MMCKINFO ckRIFF = { 0 };
        MMCKINFO ckData = { 0 };
        //     hr = WriteWaveHeader(hFile, pwfx, &ckRIFF, &ckData);
        //     if (FAILED(hr)) {
        //         // WriteWaveHeader does its own logging
        //         return hr;
        //     }

            // create a periodic waitable timer
        HANDLE hWakeUp = CreateWaitableTimer(NULL, FALSE, NULL);
        if (NULL == hWakeUp) {
            DWORD dwErr = GetLastError();
            ERR(L"CreateWaitableTimer failed: last error = %u", dwErr);
            globalErrorCondition = true;
            globalFinished = true;
            return HRESULT_FROM_WIN32(dwErr);
        }
        CloseHandleOnExit closeWakeUp(hWakeUp);

        UINT32 nBlockAlign = globalPwfx->nBlockAlign;
        *pnFrames = 0;

        // call IAudioClient::Initialize
        // note that AUDCLNT_STREAMFLAGS_LOOPBACK and AUDCLNT_STREAMFLAGS_EVENTCALLBACK
        // do not work together...
        // the "data ready" event never gets set
        // so we're going to do a timer-driven loop
        hr = pAudioClient->Initialize(
            AUDCLNT_SHAREMODE_SHARED,
            AUDCLNT_STREAMFLAGS_LOOPBACK,
            0, 0, globalPwfx, 0
        );
        if (FAILED(hr)) {
            ERR(L"IAudioClient::Initialize failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }

        ISimpleAudioVolume* pStreamVolume = nullptr;
        hr = pAudioClient->GetService(__uuidof(ISimpleAudioVolume), (void**)&pStreamVolume);
        ReleaseOnExit releaseVolumeClient(pStreamVolume);
        if (FAILED(hr)) {
            ERR(L"GetService ISimpleAudioVolume failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }

        pStreamVolume->SetMasterVolume(1.0f, nullptr);

        // activate an IAudioCaptureClient
        IAudioCaptureClient* pAudioCaptureClient;
        hr = pAudioClient->GetService(
            __uuidof(IAudioCaptureClient),
            (void**)&pAudioCaptureClient
        );
        if (FAILED(hr)) {
            ERR(L"IAudioClient::GetService(IAudioCaptureClient) failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }
        ReleaseOnExit releaseAudioCaptureClient(pAudioCaptureClient);

        // register with MMCSS
        DWORD nTaskIndex = 0;
        HANDLE hTask = AvSetMmThreadCharacteristics(L"Audio", &nTaskIndex);
        if (NULL == hTask) {
            DWORD dwErr = GetLastError();
            ERR(L"AvSetMmThreadCharacteristics failed: last error = %u", dwErr);
            globalErrorCondition = true;
            globalFinished = true;
            return HRESULT_FROM_WIN32(dwErr);
        }
        AvRevertMmThreadCharacteristicsOnExit unregisterMmcss(hTask);

        // set the waitable timer
        LARGE_INTEGER liFirstFire;
        liFirstFire.QuadPart = -hnsDefaultDevicePeriod / 2; // negative means relative time
        LONG lTimeBetweenFires = (LONG)hnsDefaultDevicePeriod / 2 / (10 * 1000); // convert to milliseconds
        BOOL bOK = SetWaitableTimer(
            hWakeUp,
            &liFirstFire,
            lTimeBetweenFires,
            NULL, NULL, FALSE
        );
        if (!bOK) {
            DWORD dwErr = GetLastError();
            ERR(L"SetWaitableTimer failed: last error = %u", dwErr);
            globalErrorCondition = true;
            globalFinished = true;
            return HRESULT_FROM_WIN32(dwErr);
        }
        CancelWaitableTimerOnExit cancelWakeUp(hWakeUp);

        // call IAudioClient::Start
        hr = pAudioClient->Start();
        if (FAILED(hr)) {
            ERR(L"IAudioClient::Start failed: hr = 0x%08x", hr);
            globalErrorCondition = true;
            globalFinished = true;
            return hr;
        }
        AudioClientStopOnExit stopAudioClient(pAudioClient);

        SetEvent(hStartedEvent);

        // loopback capture loop
        bool bFirstPacket = true;
        for (UINT32 nPasses = 0; !globalDone; nPasses++) {
            // drain data while it is available
            UINT32 nNextPacketSize;
            for (
                hr = pAudioCaptureClient->GetNextPacketSize(&nNextPacketSize);
                SUCCEEDED(hr) && nNextPacketSize > 0;
                hr = pAudioCaptureClient->GetNextPacketSize(&nNextPacketSize)
                ) {
                // get the captured data
                BYTE* pData;
                UINT32 nNumFramesToRead;
                DWORD dwFlags;

                hr = pAudioCaptureClient->GetBuffer(
                    &pData,
                    &nNumFramesToRead,
                    &dwFlags,
                    NULL,
                    NULL
                );
                if (FAILED(hr)) {
                    ERR(L"IAudioCaptureClient::GetBuffer failed on pass %u after %u frames: hr = 0x%08x", nPasses, *pnFrames, hr);
                    std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
                    globalErrorCondition = true;
                    globalFinished = true;
                    return hr;
                }

                if (AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY == dwFlags) {
                    LOG(L"%s", L"Probably spurious glitch reported on first packet");
                }
                else if (0 != dwFlags) {
                    LOG(L"IAudioCaptureClient::GetBuffer set flags to 0x%08x on pass %u after %u frames", dwFlags, nPasses, *pnFrames);
                    std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
                    globalErrorCondition = true;
                    globalFinished = true;
                    return E_UNEXPECTED;
                }

                if (0 == nNumFramesToRead) {
                    ERR(L"IAudioCaptureClient::GetBuffer said to read 0 frames on pass %u after %u frames", nPasses, *pnFrames);
                    std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
                    globalErrorCondition = true;
                    globalFinished = true;
                    return E_UNEXPECTED;
                }

                LONG lBytesToWrite = nNumFramesToRead * nBlockAlign;
                WriteBuffer(pData, lBytesToWrite);
                //#pragma prefast(suppress: __WARNING_INCORRECT_ANNOTATION, "IAudioCaptureClient::GetBuffer SAL annotation implies a 1-byte buffer")
                //             LONG lBytesWritten = mmioWrite(hFile, reinterpret_cast<PCHAR>(pData), lBytesToWrite);
                //             if (lBytesToWrite != lBytesWritten) {
                //                 ERR(L"mmioWrite wrote %u bytes on pass %u after %u frames: expected %u bytes", lBytesWritten, nPasses, *pnFrames, lBytesToWrite);
                //                 return E_UNEXPECTED;
                //             }
                *pnFrames += nNumFramesToRead;

                hr = pAudioCaptureClient->ReleaseBuffer(nNumFramesToRead);
                if (FAILED(hr)) {
                    ERR(L"IAudioCaptureClient::ReleaseBuffer failed on pass %u after %u frames: hr = 0x%08x", nPasses, *pnFrames, hr);
                    std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
                    globalErrorCondition = true;
                    globalFinished = true;
                    return hr;
                }

                bFirstPacket = false;
            }

            if (FAILED(hr) && bFirstPacket == false) {
                ERR(L"IAudioCaptureClient::GetNextPacketSize failed on pass %u after %u frames: hr = 0x%08x", nPasses, *pnFrames, hr);
                std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime));
                globalErrorCondition = true;
                globalFinished = true;
                return hr;
            }

// 
//             if (WAIT_OBJECT_0 + 1 != dwWaitResult) {
//                 ERR(L"Unexpected WaitForMultipleObjects return value %u on pass %u after %u frames", dwWaitResult, nPasses, *pnFrames);
//                 return E_UNEXPECTED;
//             }
        } // capture loop

    //     hr = FinishWaveFile(hFile, &ckData, &ckRIFF);
    //     if (FAILED(hr)) {
    //         // FinishWaveFile does it's own logging
    //         return hr;
    //     }
    //     
        std::this_thread::sleep_for(std::chrono::milliseconds(globalSleepTime)); 
        globalErrorCondition = true;
        globalFinished = true;
        return hr;
    }

}