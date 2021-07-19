// PeachComTest.cpp : Implementation of CPeachComTest

#include "stdafx.h"
#include "PeachComTest.h"
#include <string>

// We want to use unsafe strcpy() and strcat()
#pragma warning(disable: 4996)

// Tell cl.exe to let us write stack smashing bugs
#pragma warning(disable: 4789)

void Call1(char *buff)
{
	strcpy(buff, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
	for (int i = 0; i < 100; i++) {
		strcat(buff, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
	}
}

// CPeachComTest

CPeachComTest::CPeachComTest()
	: m_pTypeInfo(NULL)
{
	WCHAR szModule[MAX_PATH + 10];
	DWORD dwLen = GetModuleFileNameW((HMODULE)g_hModule, szModule, MAX_PATH);
	if (!dwLen)
		return;

	ITypeLib* pTypeLib = NULL;
	HRESULT hr = LoadTypeLib(szModule, &pTypeLib);
	if (FAILED(hr))
		return;

	pTypeLib->GetTypeInfoOfGuid(__uuidof(IPeachComTest), &m_pTypeInfo);
	pTypeLib->Release();
}

STDMETHODIMP CPeachComTest::GetTypeInfoCount(UINT* pctinfo)
{
	if (m_pTypeInfo)
		*pctinfo = 1;
	else
		*pctinfo = 0;
	return S_OK;
}

STDMETHODIMP CPeachComTest::GetTypeInfo(UINT itinfo, LCID /*lcid*/, ITypeInfo** pptinfo)
{
	if (m_pTypeInfo)
	{
		if (0 != itinfo)
			return E_INVALIDARG;
		(*pptinfo = m_pTypeInfo)->AddRef();
		return S_OK;
	}

	*pptinfo = NULL;
	return E_NOTIMPL;
}

STDMETHODIMP CPeachComTest::GetIDsOfNames(REFIID /*riid*/, LPOLESTR* rgszNames, UINT cNames, LCID /*lcid*/, DISPID* rgdispid)
{
	if (!m_pTypeInfo)
		return E_NOTIMPL;
	return m_pTypeInfo->GetIDsOfNames(rgszNames, cNames, rgdispid);
}

STDMETHODIMP CPeachComTest::Invoke(DISPID dispidMember, REFIID /*riid*/, LCID /*lcid*/, WORD wFlags, DISPPARAMS* pdispparams, VARIANT* pvarResult, EXCEPINFO* pexcepinfo, UINT* puArgErr)
{
	if (!m_pTypeInfo)
		return E_NOTIMPL;
	return m_pTypeInfo->Invoke(static_cast<IPeachComTest*>(this), dispidMember, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
}

STDMETHODIMP CPeachComTest::QueryInterface(REFIID riid, LPVOID* ppv)
{
	*ppv = NULL;
	if (IsEqualIID(riid, IID_IUnknown) ||
		IsEqualIID(riid, __uuidof(IPeachComTest)))
	{
		*ppv = (IPeachComTest*)this;
		AddRef();
		return S_OK;
	}
	if (IsEqualIID(riid, IID_IDispatch))
	{
		*ppv = (IDispatch*)this;
		AddRef();
		return S_OK;
	}
	return E_NOINTERFACE;
}

STDMETHODIMP CPeachComTest::Method1(BSTR str, BSTR* ret)
{
	wprintf(L"CPeachComTest::Method1(%s)\n", str);
	*ret = SysAllocString(L"Method1Return");
	return S_OK;
}

STDMETHODIMP CPeachComTest::Method2(BSTR* ret)
{
	printf("CPeachComTest::Method2()\n");
	*ret = SysAllocString(L"Method2Return");
	return S_OK;
}

STDMETHODIMP CPeachComTest::Method3(BSTR str)
{
	wprintf(L"CPeachComTest::Method3(%s)\n", str);

	if (wcslen(str) > 50)
	{
		char buff[10];
		wprintf(L"In Call1");
		Call1(buff);
	}

	return S_OK;
}

STDMETHODIMP CPeachComTest::Method4(void)
{
	printf("CPeachComTest::Method4()\n");
	return S_OK;
}

STDMETHODIMP CPeachComTest::get_Property1(BSTR* /*pVal*/)
{
	printf("CPeachComTest::get_Property1()\n");
	return S_OK;
}

STDMETHODIMP CPeachComTest::put_Property1(BSTR newVal)
{
	wprintf(L"CPeachComTest::put_Property1(%s)\n", newVal);
	return S_OK;
}

STDMETHODIMP CPeachComTest::Method5(LONG int1, SHORT short1, LONG* retval)
{
	*retval = int1 + short1;
	wprintf(L"CPeachComTest::Method5(%d, %d, %d)\n", int1, short1, *retval);
	return S_OK;
}

STDMETHODIMP CPeachComTest::Method6(SHORT shortParam, INT intParam)
{
	wprintf(L"CPeachComTest::Method6(%d, %d)\n", shortParam, intParam);
	return S_OK;
}
