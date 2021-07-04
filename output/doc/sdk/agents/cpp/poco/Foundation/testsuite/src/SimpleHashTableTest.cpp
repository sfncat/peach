//
// SimpleHashTableTest.cpp
//
// $Id: //poco/1.4/Foundation/testsuite/src/SimpleHashTableTest.cpp#1 $
//
// Copyright (c) 2006, Applied Informatics Software Engineering GmbH.
// and Contributors.
//
// Permission is hereby granted, free of charge, to any person or organization
// obtaining a copy of the software and accompanying documentation covered by
// this license (the "Software") to use, reproduce, display, distribute,
// execute, and transmit the Software, and to prepare derivative works of the
// Software, and to permit third-parties to whom the Software is furnished to
// do so, all subject to the following:
// 
// The copyright notices in the Software and this entire statement, including
// the above license grant, this restriction and the following disclaimer,
// must be included in all copies of the Software, in whole or in part, and
// all derivative works of the Software, unless such copies or derivative
// works are solely in the form of machine-executable object code generated by
// a source language processor.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
// SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
// FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


#include "SimpleHashTableTest.h"
#include "CppUnit/TestCaller.h"
#include "CppUnit/TestSuite.h"
#include "Poco/SimpleHashTable.h"
#include "Poco/NumberFormatter.h"


GCC_DIAG_OFF(unused-variable)


using namespace Poco;


SimpleHashTableTest::SimpleHashTableTest(const std::string& name): CppUnit::TestCase(name)
{
}


SimpleHashTableTest::~SimpleHashTableTest()
{
}


void SimpleHashTableTest::testInsert()
{
	std::string s1("str1");
	std::string s2("str2");
	SimpleHashTable<std::string, int> hashTable;
	assert (!hashTable.exists(s1));
	hashTable.insert(s1, 13);
	assert (hashTable.exists(s1));
	assert (hashTable.get(s1) == 13);
	int retVal = 0;

	assert (hashTable.get(s1, retVal));
	assert (retVal == 13);
	try
	{
		hashTable.insert(s1, 22);
		failmsg ("duplicate insert must fail");
	}
	catch (Exception&){}
	try
	{
		hashTable.get(s2);
		failmsg ("getting a non inserted item must fail");
	}
	catch (Exception&){}

	assert (!hashTable.exists(s2));
	hashTable.insert(s2, 13);
	assert (hashTable.exists(s2));
}


void SimpleHashTableTest::testUpdate()
{
	// add code for second test here
	std::string s1("str1");
	std::string s2("str2");
	SimpleHashTable<std::string, int> hashTable;
	hashTable.insert(s1, 13);
	hashTable.update(s1, 14);
	assert (hashTable.exists(s1));
	assert (hashTable.get(s1) == 14);
	int retVal = 0;

	assert (hashTable.get(s1, retVal));
	assert (retVal == 14);

	// updating a non existing item must work too
	hashTable.update(s2, 15);
	assert (hashTable.get(s2) == 15);
}


void SimpleHashTableTest::testOverflow()
{
	SimpleHashTable<std::string, int> hashTable(31);
	for (int i = 0; i < 31; ++i)
	{
		hashTable.insert(Poco::NumberFormatter::format(i), i*i);
	}

	for (int i = 0; i < 31; ++i)
	{
		std::string tmp = Poco::NumberFormatter::format(i);
		assert (hashTable.exists(tmp));
		assert (hashTable.get(tmp) == i*i);
	}
}


void SimpleHashTableTest::testSize()
{
	SimpleHashTable<std::string, int> hashTable(13);
	assert (hashTable.size() == 0);
	Poco::UInt32 h1 = hashTable.insert("1", 1);
	assert (hashTable.size() == 1);
	Poco::UInt32 h2 = hashTable.update("2", 2);
	assert (hashTable.size() == 2);
	hashTable.clear();
	assert (hashTable.size() == 0);
}


void SimpleHashTableTest::testResize()
{
	SimpleHashTable<std::string, int> hashTable(13);
	assert (hashTable.size() == 0);
	hashTable.resize(2467);
	for (int i = 0; i < 1024; ++i)
	{
		hashTable.insert(Poco::NumberFormatter::format(i), i*i);
	}
	hashTable.resize(3037);

	for (int i = 0; i < 1024; ++i)
	{
		std::string tmp = Poco::NumberFormatter::format(i);
		assert (hashTable.exists(tmp));
		assert (hashTable.get(tmp) == i*i);
	}
}


void SimpleHashTableTest::setUp()
{
}


void SimpleHashTableTest::tearDown()
{
}


CppUnit::Test* SimpleHashTableTest::suite()
{
	CppUnit::TestSuite* pSuite = new CppUnit::TestSuite("SimpleHashTableTest");

	CppUnit_addTest(pSuite, SimpleHashTableTest, testInsert);
	CppUnit_addTest(pSuite, SimpleHashTableTest, testUpdate);
	CppUnit_addTest(pSuite, SimpleHashTableTest, testOverflow);
	CppUnit_addTest(pSuite, SimpleHashTableTest, testSize);
	CppUnit_addTest(pSuite, SimpleHashTableTest, testResize);

	return pSuite;
}
