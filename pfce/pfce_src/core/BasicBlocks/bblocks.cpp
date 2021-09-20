


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

//
// PIN Tool to find all basic blocks a program hits.
//  This PIN Tool is intended for use with Peach.
//
//  Code based on examples from PIN documentation.
//

#define UNUSED_ARG(x) x;

#if defined(_MSC_VER)
#pragma warning(disable: 4127) // Conditional expression is constant

#pragma warning(push)
#pragma warning(disable: 4100) // Unreferenced formal parameter
#pragma warning(disable: 4244) // Conversion has possible loss of data
#pragma warning(disable: 4245) // Signed/unsigned mismatch
#pragma warning(disable: 4512) // Assignment operator could not be generated
#endif

#include <pin.H>

#if defined(_MSC_VER)
#pragma warning(pop)
#endif

#include <iostream>
#include <fstream>
#include <sstream>
#include <string>

#include "uthash.h"
#include "compat.h"

#define DBG(x) do { if (KnobDebug) fileDbg.Dbg x; } while (0)


class NonCopyable
{
protected:
	NonCopyable() {}
	~NonCopyable() {}

private:
	NonCopyable(const NonCopyable&);
	NonCopyable operator=(const NonCopyable&);
};

class ImageRec : NonCopyable
{
private:

public:
	typedef size_t key_type;

	ImageRec(IMG img)
		: fullName(IMG_Name(img))
		, lowAddress(IMG_LowAddress(img))
		, highAddress(IMG_HighAddress(img))
		, startAddress(IMG_StartAddress(img))
		, loadOffset(IMG_LoadOffset(img))
		, key(IMG_Id(img))
	{
	}

	const ImageRec* Next() const
	{
		return (const ImageRec*)hh.next;
	}

	const std::string fullName;    // Full absolute path to file
	const ADDRINT     lowAddress;
	const ADDRINT     highAddress;
	const ADDRINT     startAddress;
	const ADDRINT     loadOffset;

	size_t            key;
	UT_hash_handle    hh;
};

class BlockRec : NonCopyable
{
public:
	BlockRec(const ImageRec* i, BBL b)
		: image(i)
		, executed(0)
		, address(BBL_Address(b))
		, next(NULL)
	{
	}

	const ImageRec* image;
	size_t          executed;
	size_t          address;

	BlockRec       *next;
};

template<typename TVal>
class List : NonCopyable
{
public:
	List()
		: next(NULL)
	{
	}

	~List()
	{
		TVal* item = next;

		while (item != NULL)
		{
			TVal* tmp = item;
			item = item->next;
			delete tmp;
		}
	}

	void Insert(TVal* item)
	{
		item->next = next;
		next = item;
	}

	TVal *next;
};

template<typename TVal>
class HashTable : NonCopyable
{
	typedef typename TVal::key_type TKey;

public:
	HashTable()
		: table(NULL)
	{
	}

	~HashTable()
	{
		TVal *cur, *tmp;

		HASH_ITER(hh, table, cur, tmp)
		{
			HASH_DEL(table, cur);
			delete tmp;
		}
	}

	TVal* Find(const TKey& key)
	{
		return FindImpl(key);
	}

	void Add(TVal* value)
	{
		AddImpl(value->key, value);
	}

	void Remove(TVal* value)
	{
		HASH_DEL(table, value);
	}

	size_t Count() const
	{
		return HASH_COUNT(table);
	}

	const TVal* Head() const
	{
		return table;
	}

private:
	TVal* FindImpl(size_t key)
	{
		TVal* value;
		HASH_FIND_INT(table, &key, value);
		return value;
	}

	TVal* FindImpl(const std::string& key)
	{
		TVal* value;
		const char* str = key.c_str();
		size_t strlen = key.size();
		HASH_FIND(hh, table, str, (unsigned)strlen, value);
		return value;
	}

	void AddImpl(size_t, TVal* value)
	{
		HASH_ADD_INT(table, key, value);
	}

	void AddImpl(const std::string&, TVal* value)
	{
		HASH_ADD_KEYPTR(hh, table, value->key, (unsigned)value->keylen, value);
	}

	TVal*  table;
};

struct File : NonCopyable
{
public:
	File()
		: m_pFile(NULL)
	{
	}

	~File()
	{
		Close();
	}

	void Open(const std::string& name, const std::string& mode)
	{
		Close();

		std::string fullName = GetFullFileName(name);

		m_pFile = fopen(fullName.c_str(), mode.c_str());

		if (m_pFile == NULL)
		{
			OpenError(fullName, errno);
		}
		else
		{
			OpenSuccess(fullName);
		}
	}

	void Close()
	{
		if (m_pFile)
		{
			fclose(m_pFile);
			m_pFile = NULL;
		}
	}

	void Write(const std::string& value)
	{
		if (m_pFile)
		{
			fwrite(value.c_str(), 1, value.size(), m_pFile);
		}
	}

	void Write(const char* fmt, ...)
	{
		va_list args;
		va_start(args, fmt);

		if (m_pFile)
		{
			vfprintf(m_pFile, fmt, args);
		}

		va_end(args);
	}

	void Dbg(const char* fmt, ...)
	{
		va_list args;
		va_start(args, fmt);

		if (m_pFile)
		{
			char buf[2048];
			int len = vsnprintf(buf, sizeof(buf) - 2, fmt, args);
			if (len == -1)
				len = sizeof(buf) - 2;
			buf[len++] = '\n';
			buf[len] = '\0';

			fwrite(buf, 1, len, m_pFile);
			fflush(m_pFile);

			DebugWrite(buf);
		}


		va_end(args);
	}

private:
	void OpenSuccess(const std::string& name);
	void OpenError(const std::string& name, int err);

	FILE* m_pFile;
};

static HashTable<ImageRec> images;
static List<BlockRec> blocks;

File fileDbg;
INT pid = 0;

KNOB<std::string> KnobOutput(KNOB_MODE_WRITEONCE,  "pintool", "o", "bblocks", "specify base file name for output");
KNOB<BOOL> KnobDebug(KNOB_MODE_WRITEONCE, "pintool", "debug", "0", "Enable debug logging.");
KNOB<BOOL> KnobCpuKill(KNOB_MODE_WRITEONCE, "pintool", "cpukill", "0", "Kill process when cpu becomes idle.");

// Full path to the output file base
std::string OutFileBase;

void File::OpenError(const std::string& name, int err)
{
	DBG(("Failed to open file '%s': %s.", name.c_str(), strerror(err)));
}

void File::OpenSuccess(const std::string& name)
{
	DBG(("Successfully opened file '%s'.", name.c_str()));
}

// Prints the usage and exits
INT32 Usage()
{
	PIN_ERROR( "This Pintool prints a trace of all basic blocks\n"
		+ KNOB_BASE::StringKnobSummary() + "\n");
	return -1;
}

// Called whenever a basic block is executed
VOID PIN_FAST_ANALYSIS_CALL BlockExecuted(size_t* executed)
{
	*executed = 1;
}

// Called every time a new image is loaded
VOID Image(IMG img, VOID* v)
{
	UNUSED_ARG(v);

	// Using the IMG_xxx functions at the end of the tool doesn't
	// appear to work so gather all the relavant information about
	// the image at load time.

	ImageRec* pImg = new ImageRec(img);

	images.Add(pImg);

	DBG(("Loaded image %s", pImg->fullName.c_str()));
	DBG(("  Id:            " IFMT, pImg->key));
	DBG(("  Load Offset:   " XFMT, pImg->loadOffset));
	DBG(("  Low Address:   " XFMT, pImg->lowAddress));
	DBG(("  High Address:  " XFMT, pImg->highAddress));
	DBG(("  Start Address: " XFMT, pImg->startAddress));
}

// Called every time a new trace is encountered
// A trace is a single enterance, multiple exit sequence of instructions
VOID Trace(TRACE trace, VOID *v)
{
	UNUSED_ARG(v);

	RTN rtn = TRACE_Rtn(trace);
	if (!RTN_Valid(rtn))
		return;

	IMG img = SEC_Img(RTN_Sec(rtn));

	if (!IMG_Valid(img))
		return;

	IMG_TYPE imgType = IMG_Type(img);

	if ((imgType != IMG_TYPE_STATIC) &&    ///< Main image, linked with -static
		(imgType != IMG_TYPE_SHARED) &&    ///< Main image, linked against shared libraries
		(imgType != IMG_TYPE_SHAREDLIB) && ///< Shared library or main image linked with -pie
		(imgType != IMG_TYPE_RELOCATABLE)) ///< Relocatble object (.o file)
	{
		return;
	}

	const ImageRec* image = images.Find(IMG_Id(img));

	for (BBL bbl = TRACE_BblHead(trace); BBL_Valid(bbl); bbl = BBL_Next(bbl))
	{
		// Pin uniquely identifies each code block using both the starting address
		// and the register mapping upon entry to that block.  This means its possible
		// to see duplications in the starting addresses because those two blocks have
		// different register states upon entry.

		BlockRec* item = new BlockRec(image, bbl);

		blocks.Insert(item);

		// Record basic block when it is executed
		BBL_InsertCall(
			bbl,
			IPOINT_ANYWHERE,
			AFUNPTR(BlockExecuted),
			IARG_FAST_ANALYSIS_CALL,
			IARG_PTR,
			&item->executed,
			IARG_END);
	}
}

// Called when the application starts
VOID Start(VOID* v)
{
	UNUSED_ARG(v);

	pid = PIN_GetPid();

	std::string pidFile = OutFileBase + ".pid";
	std::ofstream fout(pidFile.c_str(), std::ofstream::binary | std::ofstream::trunc);
	fout << pid;

	DBG(("Application started, pid: %d", pid));
}

// Called when the application exits
VOID Fini(INT32 code, VOID *v)
{
	UNUSED_ARG(code);
	UNUSED_ARG(v);

	// Ensure exit is happening on the parent pid
	INT thisPid = PIN_GetPid();
	if (pid != thisPid)
	{
		DBG(("Child application finished, pid: %d", PIN_GetPid()));
		return;
	}

	// Open file to log new traces to
	File fileOut;
	fileOut.Open(OutFileBase + ".out", "wb");

	size_t total = 0, unresolved = 0, inavlid = 0, run = 0;

	for (const BlockRec* it = blocks.next; it != NULL; it = it->next)
	{
		++total;

		if (it->image == NULL)
		{
			++unresolved;
		}
		else if (it->executed)
		{
			if (it->image->loadOffset > it->address)
				++inavlid;
			else
				++run;

			fileOut.Write(XFMT " %s\n", it->address - it->image->loadOffset, it->image->fullName.c_str());
		}
	}

	DBG(("Application finished, pid: %d", PIN_GetPid()));
	DBG((" All Images     : " IFMT, images.Count()));
	DBG((" Basic Blocks   : " IFMT, total));
	DBG(("  Executed      : " IFMT, run));
	DBG(("  Unresolved    : " IFMT, unresolved));
	DBG(("  Invalid       : " IFMT, inavlid));
}

static INT stopping = 0;

// Called when the application exits
VOID PrepareForFini(VOID *v)
{
	UNUSED_ARG(v);
	DBG(("Preparing for finish..."));
	stopping = 1;
}

// Internal worker thread
VOID ThreadProc(VOID *v)
{
	UNUSED_ARG(v);

	uint64_t oldTicks = 0, newTicks = 0;
	bool check = false;
	int p = PIN_GetPid();

	while (!stopping)
	{
		if (!check)
			DBG(("Starting CPU thread for pid: %d", p));

		newTicks = GetProcessTicks(p);

		if (check && oldTicks == newTicks)
		{
			DBG(("Detected idle CPU after %d ticks, exiting proces", (unsigned long)oldTicks));
			PIN_ExitApplication(0);
			break;
		}

		oldTicks = newTicks;
		check = true;

		PIN_Sleep(200);
	}

	DBG(("CPU monitor thread exiting"));
	PIN_ExitThread(0);
}

int main(int argc, char* argv[])
{
	// Expect size_t and ADDRINT to be the same
	STATIC_ASSERT(sizeof(size_t) == sizeof(ADDRINT));

	// Ensure library initializes correctly
	if (PIN_Init(argc, argv))
		return Usage();

	// Get the base name to use for all outputs
	OutFileBase = GetFullFileName(KnobOutput.Value());

	if (KnobDebug)
		fileDbg.Open(OutFileBase + ".log", "wb");

	// Must be called before IMG_AddInstrumentFunction
	PIN_InitSymbols();

	// Register callbacks
	IMG_AddInstrumentFunction(Image, NULL);
	TRACE_AddInstrumentFunction(Trace, NULL);
	PIN_AddApplicationStartFunction(Start, NULL);
	PIN_AddFiniFunction(Fini, NULL);

	// Create internal thread to monitor cpu usage
	if (KnobCpuKill.Value())
	{
		PIN_AddPrepareForFiniFunction(PrepareForFini, NULL);
		PIN_SpawnInternalThread(ThreadProc, NULL, 0, NULL);
	}

	// Start program, never returns
	PIN_StartProgram();

	return 0;
}

