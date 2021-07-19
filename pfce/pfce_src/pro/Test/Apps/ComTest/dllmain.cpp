// dllmain.cpp : Implementation of DllMain.

#include "stdafx.h"
#include "ComTest_i.h"
#include "ClassFactory.h"
#include "PeachComTest.h"
#include "Registrar.h"

HANDLE g_hModule;
long g_cRefThisDll = 0;

// DLL Entry Point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID /*lpReserved*/)
{
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		g_hModule = hInstance;
		break;

	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

// Used to determine whether the DLL can be unloaded by OLE
STDAPI DllCanUnloadNow(void)
{
	return (g_cRefThisDll == 0 ? S_OK : S_FALSE);
}

// Returns a class factory to create an object of the requested type
STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppvOut)
{
	*ppvOut = NULL;
	if (IsEqualIID(rclsid, CLSID_PeachComTest))
	{
		ClassFactory<CPeachComTest>* pcf = new ClassFactory<CPeachComTest>;
		return pcf->QueryInterface(riid, ppvOut);
	}
	return CLASS_E_CLASSNOTAVAILABLE;
}

// DllRegisterServer - Adds entries to the system registry
STDAPI DllRegisterServer(void)
{
	CDllRegistrar registrar;
	char path[MAX_PATH];
	GetModuleFileName((HMODULE)g_hModule, path, MAX_PATH);
	return registrar.RegisterObject(CLSID_PeachComTest, "ComTestLib", "PeachComTest", path) ? S_OK : S_FALSE;
}

// DllUnregisterServer - Removes entries from the system registry
STDAPI DllUnregisterServer(void)
{
	CDllRegistrar registrar;
	return registrar.UnRegisterObject(CLSID_PeachComTest, "ComTestLib", "PeachComTest") ? S_OK : S_FALSE;
}

// DllInstall - Adds/Removes entries to the system registry per user
//              per machine.	
STDAPI DllInstall(BOOL bInstall, LPCWSTR /*pszCmdLine*/)
{
	HRESULT hr = E_FAIL;

	if (bInstall)
	{
		hr = DllRegisterServer();
		if (FAILED(hr))
		{
			DllUnregisterServer();
		}
	}
	else
	{
		hr = DllUnregisterServer();
	}

	return hr;
}
