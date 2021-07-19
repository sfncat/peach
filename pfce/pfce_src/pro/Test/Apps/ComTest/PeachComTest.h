// PeachComTest.h : Declaration of the CPeachComTest

#pragma once

#include "ComBase.h"
#include "ComTest_i.h"

class CPeachComTest : public ComBase<IPeachComTest>
{
public:
	CPeachComTest();

public: // IUnknown
	STDMETHODIMP QueryInterface(REFIID riid, LPVOID *ppv);

public: // IDispatch
	STDMETHOD(GetTypeInfoCount)(UINT* pctinfo);
	STDMETHOD(GetTypeInfo)(UINT itinfo, LCID lcid, ITypeInfo** pptinfo);
	STDMETHOD(GetIDsOfNames)(REFIID riid, LPOLESTR* rgszNames, UINT cNames, LCID lcid, DISPID* rgdispid);
	STDMETHOD(Invoke)(DISPID dispidMember, REFIID riid, LCID lcid, WORD wFlags, DISPPARAMS* pdispparams, VARIANT* pvarResult, EXCEPINFO* pexcepinfo, UINT* puArgErr);

public: // IPeachComTest
	STDMETHOD(Method1)(BSTR str, BSTR* ret);
	STDMETHOD(Method2)(BSTR* ret);
	STDMETHOD(Method3)(BSTR str);
	STDMETHOD(Method4)(void);
	STDMETHOD(get_Property1)(BSTR* pVal);
	STDMETHOD(put_Property1)(BSTR newVal);
	STDMETHOD(Method5)(LONG int1, SHORT short1, LONG* retval);
	STDMETHOD(Method6)(SHORT shortParam, INT intParam);

private:
	ITypeInfo* m_pTypeInfo;
};
