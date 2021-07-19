#pragma once

#include "ComBase.h"

template <class T>
class ClassFactory : public ComBase<IClassFactory>
{
public:
	ClassFactory()
	{
	}

	virtual ~ClassFactory()
	{
	}

	STDMETHOD(QueryInterface)(REFIID riid, LPVOID* ppv)
	{
		*ppv = NULL;
		if (IsEqualIID(riid, IID_IUnknown) || 
			IsEqualIID(riid, IID_IClassFactory))
		{
			*ppv = (IClassFactory*)this;
			AddRef();
			return S_OK;
		}
		return E_NOINTERFACE;
	}

	STDMETHODIMP CreateInstance(LPUNKNOWN pUnkOuter, REFIID riid, LPVOID* ppvObj)
	{
		*ppvObj = NULL;
		if (pUnkOuter)
			return CLASS_E_NOAGGREGATION;
		T* pObj = new T;
		if (!pObj)
			return E_OUTOFMEMORY;
		HRESULT hr = pObj->QueryInterface(riid, ppvObj);
		if (hr != S_OK)
			delete pObj;
		return hr;
	}

	STDMETHODIMP LockServer(BOOL) { return S_OK; }  // not implemented
};
