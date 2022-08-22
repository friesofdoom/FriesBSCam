#include "AudioCapture.h"
#include "Helpers.h"
#include "common.h"

namespace AudioCapture
{
    struct LoopbackCaptureThreadFunctionArguments {
        IMMDevice* pMMDevice;
        bool bInt16;
        HANDLE hStartedEvent;
        HANDLE hStopEvent;
        UINT32 nFrames;
        HRESULT hr;
    };

    HANDLE hThread;
    HANDLE hStartedEvent;
    LoopbackCaptureThreadFunctionArguments threadArgs;
    WAVEFORMATEX* pwfx;
    bool errorCondition = false;

    const int bufferSize = 1 * 1024 * 1024; // Must be power of 2
    int currentWritePos = 0;
    byte bigBuffer[bufferSize];
    void WriteBuffer(byte* bytes, int numBytes)
    {
        for (int i = 0; i < numBytes; i++)
        {
            int writePos = (currentWritePos + i) & (bufferSize - 1);
            bigBuffer[writePos] = bytes[i];
        }
        currentWritePos = (currentWritePos + numBytes) & (bufferSize - 1);
    }

    void ReadData(byte* data, int numBytes)
    {
        int startPos = (currentWritePos + bufferSize - numBytes) & (bufferSize - 1);
        for (int i = 0; i < numBytes; i++)
        {
            int readPos = (startPos + i) & (bufferSize - 1);
            data[i] = bigBuffer[readPos];
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
        return ::SysAllocString(Helpers::devices[index].c_str());
    }

    int GetNumDevices()
    {
        return (int)Helpers::devices.size();
    }

    BSTR GetSelectedDeviceName()
    {
        LPWSTR id;
        threadArgs.pMMDevice->GetId(&id);
        return ::SysAllocString(id);
    }

    int Init(BSTR deviceName)
    {
        errorCondition = false;
        HRESULT hr = S_OK;
        hr = CoInitialize(NULL);
        if (FAILED(hr)) {
            ERR(L"CoInitialize failed: hr = 0x%08x", hr);
            return -__LINE__;
        }

        // create a "loopback capture has started" event
        hStartedEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        if (NULL == hStartedEvent) {
            ERR(L"CreateEvent failed: last error is %u", GetLastError());
            return -__LINE__;
        }
        CloseHandleOnExit closeStartedEvent(hStartedEvent);

        // create a "stop capturing now" event
        HANDLE hStopEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        if (NULL == hStopEvent) {
            ERR(L"CreateEvent failed: last error is %u", GetLastError());
            return -__LINE__;
        }
        CloseHandleOnExit closeStopEvent(hStopEvent);

        // create arguments for loopback capture thread
        threadArgs.hr = E_UNEXPECTED; // thread will overwrite this
        HRESULT res = Helpers::get_specific_device(deviceName, &threadArgs.pMMDevice);

        if (res != S_OK)
        {
            res = Helpers::get_default_device(&threadArgs.pMMDevice);
            if (res != S_OK)
            {
                return -__LINE__;
            }
        }

        threadArgs.bInt16 = true;
        threadArgs.hStartedEvent = hStartedEvent;
        threadArgs.hStopEvent = hStopEvent;
        threadArgs.nFrames = 0;

        hThread = CreateThread(
            NULL, 0,
            LoopbackCaptureThreadFunction, &threadArgs,
            0, NULL
        );
        if (NULL == hThread) {
            ERR(L"CreateThread failed: last error is %u", GetLastError());
            return -__LINE__;
        }

        Helpers::list_devices();

        return 0;
    }

    int Shutdown()
    {
        if (!CloseHandle(hThread)) {
            ERR(L"CloseHandle failed: last error is %d", GetLastError());
        }

        // wait for either capture to start or the thread to end
        HANDLE waitArray[2] = { hStartedEvent, hThread };
        DWORD dwWaitResult;
        dwWaitResult = WaitForMultipleObjects(
            ARRAYSIZE(waitArray), waitArray,
            FALSE, INFINITE
        );

        if (WAIT_OBJECT_0 + 1 == dwWaitResult) {
            ERR(L"Thread aborted before starting to loopback capture: hr = 0x%08x", threadArgs.hr);
            return -__LINE__;
        }

        if (WAIT_OBJECT_0 != dwWaitResult) {
            ERR(L"Unexpected WaitForMultipleObjects return value %u", dwWaitResult);
            return -__LINE__;
        }


        DWORD exitCode;
        if (!GetExitCodeThread(hThread, &exitCode)) {
            ERR(L"GetExitCodeThread failed: last error is %u", GetLastError());
            return -__LINE__;
        }

        if (0 != exitCode) {
            ERR(L"Loopback capture thread exit code is %u; expected 0", exitCode);
            return -__LINE__;
        }

        if (S_OK != threadArgs.hr) {
            ERR(L"Thread HRESULT is 0x%08x", threadArgs.hr);
            return -__LINE__;
        }

        CoUninitialize();

        return 0;
    }

    int GetNumChannels()
    {
        return pwfx == nullptr ? 0 : pwfx->nChannels;
    }

    bool HasError()
    {
        return errorCondition;
    }


    DWORD WINAPI LoopbackCaptureThreadFunction(LPVOID pContext) {
        LoopbackCaptureThreadFunctionArguments* pArgs =
            (LoopbackCaptureThreadFunctionArguments*)pContext;

        pArgs->hr = CoInitialize(NULL);
        if (FAILED(pArgs->hr)) {
            ERR(L"CoInitialize failed: hr = 0x%08x", pArgs->hr);
            return 0;
        }
        CoUninitializeOnExit cuoe;

        pArgs->hr = LoopbackCapture(
            pArgs->pMMDevice,
            pArgs->bInt16,
            pArgs->hStartedEvent,
            pArgs->hStopEvent,
            &pArgs->nFrames
        );

        return 0;
    }

    HRESULT LoopbackCapture(
        IMMDevice* pMMDevice,
        bool bInt16,
        HANDLE hStartedEvent,
        HANDLE hStopEvent,
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
            return hr;
        }
        ReleaseOnExit releaseAudioClient(pAudioClient);

        // get the default device periodicity
        REFERENCE_TIME hnsDefaultDevicePeriod;
        hr = pAudioClient->GetDevicePeriod(&hnsDefaultDevicePeriod, NULL);
        if (FAILED(hr)) {
            ERR(L"IAudioClient::GetDevicePeriod failed: hr = 0x%08x", hr);
            return hr;
        }

        // get the default device format
        hr = pAudioClient->GetMixFormat(&pwfx);
        if (FAILED(hr)) {
            ERR(L"IAudioClient::GetMixFormat failed: hr = 0x%08x", hr);
            return hr;
        }
        CoTaskMemFreeOnExit freeMixFormat(pwfx);

        if (bInt16) {
            // coerce int-16 wave format
            // can do this in-place since we're not changing the size of the format
            // also, the engine will auto-convert from float to int for us
            switch (pwfx->wFormatTag) {
            case WAVE_FORMAT_IEEE_FLOAT:
                pwfx->wFormatTag = WAVE_FORMAT_PCM;
                pwfx->wBitsPerSample = 16;
                pwfx->nBlockAlign = pwfx->nChannels * pwfx->wBitsPerSample / 8;
                pwfx->nAvgBytesPerSec = pwfx->nBlockAlign * pwfx->nSamplesPerSec;
                break;

            case WAVE_FORMAT_EXTENSIBLE:
            {
                // naked scope for case-local variable
                PWAVEFORMATEXTENSIBLE pEx = reinterpret_cast<PWAVEFORMATEXTENSIBLE>(pwfx);
                if (IsEqualGUID(KSDATAFORMAT_SUBTYPE_IEEE_FLOAT, pEx->SubFormat)) {
                    pEx->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
                    pEx->Samples.wValidBitsPerSample = 16;
                    pwfx->wBitsPerSample = 16;
                    pwfx->nBlockAlign = pwfx->nChannels * pwfx->wBitsPerSample / 8;
                    pwfx->nAvgBytesPerSec = pwfx->nBlockAlign * pwfx->nSamplesPerSec;
                }
                else {
                    ERR(L"%s", L"Don't know how to coerce mix format to int-16");
                    return E_UNEXPECTED;
                }
            }
            break;

            default:
                ERR(L"Don't know how to coerce WAVEFORMATEX with wFormatTag = 0x%08x to int-16", pwfx->wFormatTag);
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
            return HRESULT_FROM_WIN32(dwErr);
        }
        CloseHandleOnExit closeWakeUp(hWakeUp);

        UINT32 nBlockAlign = pwfx->nBlockAlign;
        *pnFrames = 0;

        // call IAudioClient::Initialize
        // note that AUDCLNT_STREAMFLAGS_LOOPBACK and AUDCLNT_STREAMFLAGS_EVENTCALLBACK
        // do not work together...
        // the "data ready" event never gets set
        // so we're going to do a timer-driven loop
        hr = pAudioClient->Initialize(
            AUDCLNT_SHAREMODE_SHARED,
            AUDCLNT_STREAMFLAGS_LOOPBACK,
            0, 0, pwfx, 0
        );
        if (FAILED(hr)) {
            ERR(L"IAudioClient::Initialize failed: hr = 0x%08x", hr);
            return hr;
        }

        // activate an IAudioCaptureClient
        IAudioCaptureClient* pAudioCaptureClient;
        hr = pAudioClient->GetService(
            __uuidof(IAudioCaptureClient),
            (void**)&pAudioCaptureClient
        );
        if (FAILED(hr)) {
            ERR(L"IAudioClient::GetService(IAudioCaptureClient) failed: hr = 0x%08x", hr);
            return hr;
        }
        ReleaseOnExit releaseAudioCaptureClient(pAudioCaptureClient);

        // register with MMCSS
        DWORD nTaskIndex = 0;
        HANDLE hTask = AvSetMmThreadCharacteristics(L"Audio", &nTaskIndex);
        if (NULL == hTask) {
            DWORD dwErr = GetLastError();
            ERR(L"AvSetMmThreadCharacteristics failed: last error = %u", dwErr);
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
            return HRESULT_FROM_WIN32(dwErr);
        }
        CancelWaitableTimerOnExit cancelWakeUp(hWakeUp);

        // call IAudioClient::Start
        hr = pAudioClient->Start();
        if (FAILED(hr)) {
            ERR(L"IAudioClient::Start failed: hr = 0x%08x", hr);
            return hr;
        }
        AudioClientStopOnExit stopAudioClient(pAudioClient);

        SetEvent(hStartedEvent);

        // loopback capture loop
        HANDLE waitArray[2] = { hStopEvent, hWakeUp };
        DWORD dwWaitResult;

        bool bDone = false;
        bool bFirstPacket = true;
        for (UINT32 nPasses = 0; !bDone; nPasses++) {
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
                    errorCondition = true;
                    return hr;
                }

                if (AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY == dwFlags) {
                    LOG(L"%s", L"Probably spurious glitch reported on first packet");
                }
                else if (0 != dwFlags) {
                    LOG(L"IAudioCaptureClient::GetBuffer set flags to 0x%08x on pass %u after %u frames", dwFlags, nPasses, *pnFrames);
                    errorCondition = true;
                    return E_UNEXPECTED;
                }

                if (0 == nNumFramesToRead) {
                    ERR(L"IAudioCaptureClient::GetBuffer said to read 0 frames on pass %u after %u frames", nPasses, *pnFrames);
                    errorCondition = true;
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
                    errorCondition = true;
                    return hr;
                }

                bFirstPacket = false;
            }

            if (FAILED(hr)) {
                ERR(L"IAudioCaptureClient::GetNextPacketSize failed on pass %u after %u frames: hr = 0x%08x", nPasses, *pnFrames, hr);
                return hr;
            }

            dwWaitResult = WaitForMultipleObjects(
                ARRAYSIZE(waitArray), waitArray,
                FALSE, INFINITE
            );

//             if (WAIT_OBJECT_0 == dwWaitResult) {
//                 LOG(L"Received stop event after %u passes and %u frames", nPasses, *pnFrames);
//                 bDone = true;
//                 continue; // exits loop
//             }
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
        return hr;
    }

}