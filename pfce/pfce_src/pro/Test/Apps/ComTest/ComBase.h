#pragma once
#include <objbase.h>

extern HANDLE g_hModule;
extern long g_cRefThisDll;

template <class Interface>
class ComBase : public Interface
{
public:
	ComBase()
		: m_cRef(0)
	{
	}
	
	virtual ~ComBase()
	{
	}

	STDMETHOD(QueryInterface)(REFIID riid, LPVOID* ppv) = 0;

	STDMETHODIMP_(ULONG) AddRef()
	{
		InterlockedIncrement(&g_cRefThisDll);
		return InterlockedIncrement(&m_cRef);
	}

	STDMETHODIMP_(ULONG) Release()
	{
		long value = InterlockedDecrement(&m_cRef);
		if (!m_cRef)
			delete this;
		InterlockedDecrement(&g_cRefThisDll);
		return value;
	}

protected:
	long m_cRef;
};
