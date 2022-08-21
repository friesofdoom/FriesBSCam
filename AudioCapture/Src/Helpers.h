#pragma once
#include "common.h"
#include <vector>
#include <string>

class Helpers
{
public:
    static std::vector<std::wstring> devices;
    static HRESULT get_default_device(IMMDevice** ppMMDevice);
    static HRESULT list_devices();
    static HRESULT get_specific_device(LPCWSTR szLongName, IMMDevice** ppMMDevice);
};