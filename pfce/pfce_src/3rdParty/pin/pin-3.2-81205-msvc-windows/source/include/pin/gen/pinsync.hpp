/*BEGIN_LEGAL 
Intel Open Source License 

Copyright (c) 2002-2016 Intel Corporation. All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.  Redistributions
in binary form must reproduce the above copyright notice, this list of
conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.  Neither the name of
the Intel Corporation nor the names of its contributors may be used to
endorse or promote products derived from this software without
specific prior written permission.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
END_LEGAL */
// <COMPONENT>: pinvm
// <FILE-TYPE>: public header

#ifndef PINSYNC_HPP
#define PINSYNC_HPP

#include "os-apis.h"
#include <map>
#include <string>

namespace PINVM {
/*!
 * Basic non-recursive lock.
 */
class PINSYNC_LOCK /*<UTILITY>*/
{
public:
    /*!
     * The initial sate of the lock is "not locked".
     */
    PINSYNC_LOCK() { OS_MutexInit(&_impl); }

    /*!
     * It is not necessary to call this method.  It is provided only for symmetry.
     *
     * @return  Always returns TRUE.
     */
    bool Initialize() { OS_MutexInit(&_impl); return true; }

    /*!
     * Destroy a mutex
     */
    void Destroy() { OS_MutexDestroy(&_impl); }

    /*!
     * Set the state of the lock to "not locked", even if the calling thread
     * does not own the lock.
     */
    void Reset() { OS_MutexUnlock(&_impl); }

    /*!
     * Blocks the caller until the lock can be acquired.
     */
    void Lock() { OS_MutexLock(&_impl); }

    /*!
     * Releases the lock.
     */
    void Unlock() { OS_MutexUnlock(&_impl); }

    /*!
     * Attempts to acquire the lock, but does not block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    bool TryLock() {return OS_MutexTryLock(&_impl); }

private:
    OS_MUTEX_TYPE _impl;
};

/*!
 * Basic non-recursive lock with POD semantics.
 */
typedef struct
{
public:
    /*!
     * It is not necessary to call this method.  It is provided only for symmetry.
     *
     * @return  Always returns TRUE.
     */
    bool Initialize() { OS_MutexInit(&_impl); return true; }

    /*!
     * Destroy a mutex
     */
    void Destroy() { OS_MutexDestroy(&_impl); }

    /*!
     * Set the state of the lock to "not locked", even if the calling thread
     * does not own the lock.
     */
    void Reset() { OS_MutexUnlock(&_impl); }

    /*!
     * Blocks the caller until the lock can be acquired.
     */
    void Lock() { OS_MutexLock(&_impl); }

    /*!
     * Releases the lock.
     */
    void Unlock() { OS_MutexUnlock(&_impl); }

    /*!
     * Attempts to acquire the lock, but does not block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    bool TryLock() {return OS_MutexTryLock(&_impl); }

    OS_MUTEX_TYPE _impl;
} PINSYNC_POD_LOCK;

/*!
 * Basic non-recursive lock with SAFEPOD semantics.
 */
typedef PINSYNC_POD_LOCK PINSYNC_SAFEPOD_LOCK;

/*!
 * Read-writer lock.
 */
class PINSYNC_RWLOCK /*<UTILITY>*/
{
public:
    /*!
     * The new lock is initially not acquired
     */
    PINSYNC_RWLOCK() { OS_RWLockInitialize(&_impl); }

    /*!
     * It is not necessary to call this method.  It is provided only for symmetry.
     *
     * @return  Always returns TRUE.
     */
    bool Initialize() { OS_RWLockInitialize(&_impl); return true;}

    /*!
     * Destroy a read-writer lock
     */
    void Destroy() { OS_RWLockDestroy(&_impl); }

    /*!
     * Set the state of the lock to "not locked", even if the calling thread
     * does not own the lock.
     */
    void Reset() { OS_RWLockRelease(&_impl); }

    /*!
     * Acquire the lock for "read" access.  Multiple "reader" threads may
     * simultaneously acquire the lock.
     */
    void ReadLock() { OS_RWLockAcquireRead(&_impl); }

    /*!
     * Acquire the lock for exclusive "write" access.  A "writer" thread has
     * exclusive ownership of the lock, not shared by any other "reader" or
     * "writer" threads.
     */
    void WriteLock() { OS_RWLockAcquireWrite(&_impl); }

    /*!
     * Release the lock.  Used for both "readers" and "writers".
     */
    void Unlock() { OS_RWLockRelease(&_impl); }

    /*!
     * Attempts to acquire the lock as a "reader" thread, but does not
     * block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    bool TryReadLock() { return OS_RWLockTryAcquireRead(&_impl); }

    /*!
     * Attempts to acquire the lock as an exclusive "writer" thread, but
     * does not block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    bool TryWriteLock() { return OS_RWLockTryAcquireWrite(&_impl); }

private:
    OS_APIS_RW_LOCK_T _impl;
};

/*!
 * Read-writer lock with POD semantics.
 */
typedef struct
{
public:
    /*!
     * It is not necessary to call this method.  It is provided only for symmetry.
     *
     * @return  Always returns TRUE.
     */
    bool Initialize() { OS_RWLockInitialize(&_impl); return true;}

    /*!
     * Destroy a read-writer lock
     */
    void Destroy() { OS_RWLockDestroy(&_impl); }

    /*!
     * Set the state of the lock to "not locked", even if the calling thread
     * does not own the lock.
     */
    void Reset() { OS_RWLockRelease(&_impl); }

    /*!
     * Acquire the lock for "read" access.  Multiple "reader" threads may
     * simultaneously acquire the lock.
     */
    void ReadLock() { OS_RWLockAcquireRead(&_impl); }

    /*!
     * Acquire the lock for exclusive "write" access.  A "writer" thread has
     * exclusive ownership of the lock, not shared by any other "reader" or
     * "writer" threads.
     */
    void WriteLock() { OS_RWLockAcquireWrite(&_impl); }

    /*!
     * Release the lock.  Used for both "readers" and "writers".
     */
    void Unlock() { OS_RWLockRelease(&_impl); }

    /*!
     * Attempts to acquire the lock as a "reader" thread, but does not
     * block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    bool TryReadLock() { return OS_RWLockTryAcquireRead(&_impl); }

    /*!
     * Attempts to acquire the lock as an exclusive "writer" thread, but
     * does not block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    bool TryWriteLock() { return OS_RWLockTryAcquireWrite(&_impl); }

    OS_APIS_RW_LOCK_T _impl;
} PINSYNC_POD_RWLOCK;


/*!
 * Recursive Read-writer lock.
 */
class PINSYNC_RECURSIVE_RWLOCK /*<UTILITY>*/
{
public:
    /*!
     * The new lock is initially not acquired
     */
    PINSYNC_RECURSIVE_RWLOCK()
    {
        _writer_tid = INVALID_NATIVE_TID;
        _write_recursion_level = 0;
        _read_recursion_level.clear();
        _impl.Initialize();
        _mutex.Initialize();
        _snapshots.clear();
        _pausedInAtLeastOneThread = false;
        _dependent_lock = NULL;
    }

    virtual ~PINSYNC_RECURSIVE_RWLOCK()
    {
        _impl.Destroy();
        _mutex.Destroy();
    }

     /*!
     * Define a hierarchy between this lock and another lock.
     * This lock must not be acquired if the dependent lock is already locked.
     */
    void SetDependentLock(PINSYNC_RECURSIVE_RWLOCK* dependent_lock)
    {
        _dependent_lock = dependent_lock;
    }

    /*!
     * Set the state of the lock to "not locked", even if the calling thread
     * does not own the lock.
      */
    void Reset()
    {
        _writer_tid = INVALID_NATIVE_TID;
        _write_recursion_level = 0;
        _read_recursion_level.clear();
        _impl.Reset();
        _mutex.Reset();
        _snapshots.clear();
        _pausedInAtLeastOneThread = false;
        _dependent_lock = NULL;
    }

    /*!
     * Acquire the lock for "read" access.  Multiple "reader" threads may
     * simultaneously acquire the lock.
     * To prevent a thread from deadlocking on itself, only nested Read locks are allowed.
     */
    virtual void ReadLock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        ReadLock(tid);
    }

    /*!
     * Acquire the lock for exclusive "write" access.  A "writer" thread has
     * exclusive ownership of the lock, not shared by any other "reader" or
     * "writer" threads.
     * To prevent a thread from deadlocking on itself, once a thread took a Write lock,
     * and for as long as it is locked, it may not take another lock, neither Read nor Write.
     */
    virtual void WriteLock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        WriteLock(tid);
    }

    /*!
     * Release the lock.  Used for both "readers" and "writers".
     */
    virtual void Unlock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        Unlock(tid);
    }

    /*!
     * Release the lock if it is currently locked.  Used for both "readers" and "writers".
     */
    virtual void UnlockIfLocked()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        UnlockIfLocked(tid);
    }

    /*!
     * Save the state of all locks (either reader and writer locks) acquired by the current thread,
       then release these locks so other threads can acquire the lock.
       The state of all locks in saved in a way that allows the current thread to later
       re-claim the exact locks when calling ResumeLock().
     */
    virtual void PauseLock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        PauseLock(tid);
    }

    /*!
     * Restore the lock state saved by the call to PauseLock() and acquire the lock.
     * If the lock had both active Write locks and Read locks then the Write lock takes precedence
     * since only Write can be at the top of the lock chain.
     */
    virtual void ResumeLock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsLegalLockChain(tid) );
        ResumeLock(tid);
    }

    /*!
     * Attempts to acquire the lock as a "reader" thread, but does not
     * block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    virtual bool TryReadLock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        return TryReadLock();
    }

    /*!
     * Attempts to acquire the lock as an exclusive "writer" thread, but
     * does not block the caller.
     *
     * @return  Returns TRUE if the lock is acquired, FALSE if not.
     */
    virtual bool TryWriteLock()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        ASSERTX( IsPaused(tid) == false );
        ASSERTX( IsLegalLockChain(tid) );
        return TryWriteLock(tid);
    }

    /*!
     * Returns TRUE if the current thread has acquired either a Read lock or Write lock.
     *
     * @return  Returns TRUE if a Read/Write lock is acquired, FALSE if not.
     */
    virtual bool IsLockedByCurrentThread()
    {
        NATIVE_TID tid;
        ASSERTX( OS_RETURN_CODE_IS_SUCCESS(OS_GetTid(&tid)) );
        return IsLocked(tid);
    }

private:

    void ReadLock(NATIVE_TID tid)
    {
        int recursion_level;
        ASSERTX( MayAcquireReadLock(tid, &recursion_level) );
        if (recursion_level == 0)
            _impl.ReadLock();
        RegisterReadLock(tid);
    }

    void WriteLock(NATIVE_TID tid)
    {
        int recursion_level;
        ASSERTX( MayAcquireWriteLock(tid, &recursion_level) );
        if (recursion_level == 0)
            _impl.WriteLock();
        RegisterWriteLock(tid);
    }

    void Unlock(NATIVE_TID tid)
    {
        Unlock(tid, true);
    }

    void UnlockIfLocked(NATIVE_TID tid)
    {
        Unlock(tid, false);
    }

    void Unlock(NATIVE_TID tid, bool assert_is_locked)
    {
        int recursion_level=0;
        // Unregister Read locks first because only Write can be at the top of the lock chain
        bool lock_unregistered = UnregisterReadLock(tid, &recursion_level);
        if (lock_unregistered )
        {
            if (recursion_level==0)
                recursion_level = GetWriteRecursionLevel(tid);
        }
        else
        {
            lock_unregistered = UnregisterWriteLock(tid, &recursion_level);
        }
        ASSERTX( (assert_is_locked==false) || lock_unregistered );
        if (lock_unregistered && (recursion_level==0))
            _impl.Unlock();
    }

    void PauseLock(NATIVE_TID tid)
    {
        _mutex.Lock();
        std::map<NATIVE_TID, int>::iterator it_rd = _read_recursion_level.find(tid);
        int rd_recursion = ((it_rd != _read_recursion_level.end()) && (it_rd->second >= 1)) ? it_rd->second : 0;
        int wr_recursion = (_writer_tid == tid) ? _write_recursion_level : 0;
        if ((rd_recursion==0) && (wr_recursion==0))
        {
            _mutex.Unlock();
            return;
        }
        std::map<NATIVE_TID, Snapshot*>::iterator it_snp = _snapshots.find(tid);
        ASSERTX(it_snp == _snapshots.end());
        _snapshots.insert(std::make_pair(tid, new Snapshot(rd_recursion, wr_recursion)));
        _pausedInAtLeastOneThread = true;
        if (wr_recursion >= 1)
        {
            _write_recursion_level = 1;
            it_rd->second = 0;
        }
        else
        {
            it_rd->second = 1;
        }
        _mutex.Unlock();
        Unlock(tid);
    }

    void ResumeLock(NATIVE_TID tid)
    {
        _mutex.Lock();
        std::map<NATIVE_TID, Snapshot*>::iterator it_snp = _snapshots.find(tid);
        if (it_snp == _snapshots.end())
        {
            _mutex.Unlock();
            return;
        }
        int rd_recursion = (it_snp->second)->read_recursion_level;
        int wr_recursion = (it_snp->second)->write_recursion_level;
        _snapshots.erase(it_snp);
        _pausedInAtLeastOneThread = _snapshots.size()>0;
        _mutex.Unlock();

        if (wr_recursion)
        {
            WriteLock(tid);
            wr_recursion--;
        }
        else if (rd_recursion)
        {
            ReadLock(tid);
            rd_recursion--;
        }

        for (int i=0; i < wr_recursion; i++)
            RegisterWriteLock(tid);

        for (int i=0; i < rd_recursion; i++)
            RegisterReadLock(tid);
    }

    bool TryReadLock(NATIVE_TID tid)
    {
        int recursion_level;
        ASSERTX( MayAcquireReadLock(tid, &recursion_level) );
        if (recursion_level == 0)
            if (! _impl.TryReadLock())
                return false;

        RegisterReadLock(tid);
        return true;
    }

    bool TryWriteLock(NATIVE_TID tid)
    {
        int recursion_level;
        ASSERTX( MayAcquireWriteLock(tid, &recursion_level) );
        if (recursion_level == 0)
            if (! _impl.TryWriteLock())
                return false;

        RegisterWriteLock(tid);
        return true;
    }
    /*!
     * Checks the conditions for whether it is legal to take a Read lock for this thread.
     * Taking a Read lock is always legal.
     * Only the first lock in the recursion chain for the current thread will be physically taken.
     * If the current thread has an active Write lock then implicitly no other thread will modify
     * the data so the requirement for the Read lock is met.
     * If the current thread has an active Read lock then it is legal to take another Read lock.
     *
     * @param[in]   tid                     The thread Id
     * @param[out] recursionLevel     The current recursion level, read + write.

     * @return  Returns TRUE if may acquire Read lock, FALSE if not.
     */
    bool MayAcquireReadLock(NATIVE_TID tid, int* recursionLevel)
    {
        _mutex.Lock();
        std::map<NATIVE_TID, int>::const_iterator it = _read_recursion_level.find(tid);
        *recursionLevel = (it == _read_recursion_level.end()) ? 0 : it->second;
        if (tid == _writer_tid)
            *recursionLevel += _write_recursion_level;
        _mutex.Unlock();
        return true;
    }

    /*!
     * Checks the conditions for whether it is legal to take a Write lock for this thread.
     * Taking a Write lock is legal only if this thread does not have an active Read physical lock.
     * If the thread has an active Write lock, it is legal to take another Write lock.
     *
     * @param[in]   tid                     The thread Id
     * @param[out] recursionLevel     The current recursion level, read + write.

     * @return  Returns TRUE if may acquire Write lock, FALSE if not.
     */
    bool MayAcquireWriteLock(NATIVE_TID tid, int* recursionLevel)
    {
        bool ret = false;
        *recursionLevel = 0;
        _mutex.Lock();
        std::map<NATIVE_TID, int>::const_iterator it = _read_recursion_level.find(tid);
        if ((tid == _writer_tid) || (it == _read_recursion_level.end()) || (it->second==0))
        {
            ret = true;
            if (tid == _writer_tid)
                *recursionLevel += _write_recursion_level;
            if (it != _read_recursion_level.end())
                *recursionLevel += it->second;
        }
        _mutex.Unlock();
         return ret;
    }

    /*!
     * Register a Read lock.
     */
    void RegisterReadLock(NATIVE_TID tid)
    {
        _mutex.Lock();
        std::map<NATIVE_TID, int>::iterator it = _read_recursion_level.find(tid);
        if (it != _read_recursion_level.end())
            (it->second)++;
        else
            _read_recursion_level.insert(std::make_pair(tid, 1));
        _mutex.Unlock();
    }

    /*!
     * Unregister a Read lock, if one exists for tid.
     *
     * @param[in]   tid                     The thread Id
     * @param[out] recursionLevel      The Read recursion level after unregister, or 0 if there was no Read lock for tid.
     *
     * @return  Return true if this tid had an active Read lock, false otherwise.
     */
    bool UnregisterReadLock(NATIVE_TID tid, int* recursionLevel)
    {
        bool is_locked = false;
        *recursionLevel = 0;
        _mutex.Lock();
        std::map<NATIVE_TID, int>::iterator it = _read_recursion_level.find(tid);
        if ( (it != _read_recursion_level.end()) && (it->second >= 1) )
        {
            is_locked = true;
            *recursionLevel = it->second - 1;
            if (it->second == 1)
                _read_recursion_level.erase(it);
            else
                (it->second)--;
        }
        _mutex.Unlock();
        return is_locked;
    }

    /*!
     * Register a Write lock.
     */
    void RegisterWriteLock(NATIVE_TID tid)
    {
        _mutex.Lock();
        _writer_tid = tid;
        _write_recursion_level++;
        _mutex.Unlock();
    }

    /*!
     * Unregister a Write lock, if one exists for tid.
     *
     * @param[in]   tid                     The thread Id
     * @param[out] recursionLevel      The Write recursion level after unregister, or 0 if there was no Write lock for tid.
     *
     * @return  Return true if this tid had an active Write lock, false otherwise.
     */
    bool UnregisterWriteLock(NATIVE_TID tid, int* recursionLevel)
    {
        bool is_locked = false;
        *recursionLevel = 0;
        _mutex.Lock();
        if (_writer_tid == tid)
        {
            is_locked = true;
            _write_recursion_level--;
            *recursionLevel = _write_recursion_level;

            if (_write_recursion_level == 0)
            {
                _writer_tid = INVALID_NATIVE_TID;
            }
        }
        _mutex.Unlock();
        return is_locked;
    }

    /*!
     * Get the current write recursion level for tid
     *
     * @param[in]   tid                     The thread Id
     *
     * @return  Return 0 if tid has no active write lock, otherwise returns the write lock recursion level.
     */
    int GetWriteRecursionLevel(NATIVE_TID tid)
    {
        _mutex.Lock();
        int recursionLevel = (_writer_tid == tid) ? _write_recursion_level : 0;
        _mutex.Unlock();
        return recursionLevel;
    }

    /*!
     * Checks whether the current thread has acquired either a Read or Write lock
     *
     * @return  Returns TRUE if the current thread has either a Read or Write lock. FALSE otherwise.
     */
    bool IsLocked(NATIVE_TID tid)
    {
        bool is_locked = false;
        _mutex.Lock();
        if (_writer_tid == tid)
        {
            is_locked = true;
        }
        else
        {
            std::map<NATIVE_TID, int>::const_iterator it = _read_recursion_level.find(tid);
            is_locked = (it != _read_recursion_level.end()) && (it->second > 0);
        }
        _mutex.Unlock();
        return is_locked;
    }

    /*!
     * Checks whether the lock for tid is in paused state.
     *
     * @return  Returns TRUE if the lock for tid is in paused state, FALSE otherwise.
     */
    bool IsPaused(NATIVE_TID tid)
    {
        if (! _pausedInAtLeastOneThread)
            return false;

        bool is_paused = false;
        _mutex.Lock();
        std::map<NATIVE_TID, Snapshot*>::const_iterator it = _snapshots.find(tid);
        is_paused = (it != _snapshots.end());
        _mutex.Unlock();
        return is_paused;
    }

    /*!
     * Checks whether the lock chain is legal.
     * Conditions for a legal lock chain:
     * 1. There is no dependency on another lock.
     * 2. This lock is already locked. In this case it doesn't matter whether the dependent
     *     lock is locked or not because we know that we will not take another physical lock.
     * 3. The dependent lock is not in locked state.
     *
     * @return  Returns TRUE if the lock chain is legal, FALSE otherwise.
     */
    bool IsLegalLockChain(NATIVE_TID tid)
    {
        if (_dependent_lock==NULL)
            return true;
        if (IsLocked(tid))
            return true;
        if (_dependent_lock->IsLockedByCurrentThread()==false)
            return true;
        return false;
    }

    PINSYNC_POD_RWLOCK  _impl;
    NATIVE_TID _writer_tid;
    int _write_recursion_level;
    std::map<NATIVE_TID, int> _read_recursion_level;
    PINSYNC_POD_LOCK _mutex;
    struct Snapshot
    {
        int read_recursion_level;
        int write_recursion_level;
        Snapshot(int rd_rec, int wr_rec) : read_recursion_level(rd_rec), write_recursion_level(wr_rec) {}
    };
    std::map<NATIVE_TID, Snapshot*> _snapshots;
    bool _pausedInAtLeastOneThread;
    PINSYNC_RECURSIVE_RWLOCK* _dependent_lock;
 };




/*!
 * Binary semaphore.
 * This synchronization object works as a barrier.
 */
class PINSYNC_SEMAPHORE /*<UTILITY>*/
{
public:
    /*!
     * The initial state of the semaphore is "clear".
     */
    PINSYNC_SEMAPHORE() { OS_MutexInit(&_impl); Clear(); }

    /*!
     * It is not necessary to call this method.  It is provided only for symmetry.
     *
     * @return  Always returns TRUE.
     */
    bool Initialize() { OS_MutexInit(&_impl); Clear(); return true; }

    /*!
     * Destroy a semaphore
     */
    void Destroy() {OS_MutexDestroy(&_impl); }

    /*!
     * Change the semaphore to "set" state and tell any waiters in Wait() or
     * TimedWait() to resume.  Those threads are guaranteed to return from
     * Wait() or TimedWait() only if the semaphore is still "set" when they
     * actually do resume running.
     */
    void Set() { OS_MutexUnlock(&_impl); }

    /*!
     * Change the semaphore to "clear" state.
     */
    void Clear() { OS_MutexTryLock(&_impl); }

    /*!
     * Check whether the semaphore's state is "set".  This method always returns
     * immediately.
     *
     * @return  TRUE if the state is "set".
     */
    bool IsSet() { return !OS_MutexIsLocked(&_impl); }

    /*!
     * Block the calling thread until the semaphore's state is "set".  This
     * method returns immediately if the state is already "set".
     */
    void Wait() { OS_MutexLock(&_impl); OS_MutexUnlock(&_impl); }

    /*!
     * Block the calling thread until the semaphore's state is "set" or until
     * a timeout expires.  This method returns immediately if the state is
     * already "set".
     *
     *  @param[in] timeout  Maximum number of milliseconds to wait.
     *
     * @return  TRUE if the semaphore is in "set" state.  FALSE if this method
     *           returns instead due to the timeout.
     */
    bool TimedWait(unsigned timeout)
    {
        bool res = OS_MutexTimedLock(&_impl, timeout);
        if (res) OS_MutexUnlock(&_impl);
        return res;
    }

private:
    OS_MUTEX_TYPE _impl;
};

/*!
 * Binary semaphore with POD semantics.
 * This synchronization object works as a barrier.
 */
typedef struct
{
public:
    /*!
     * Initialize a semaphore
     *
     * @return  Always returns TRUE.
     */
    bool Initialize() { OS_MutexInit(&_impl); Clear(); return true; }

    /*!
     * Destroy a semaphore
     */
    void Destroy() {OS_MutexDestroy(&_impl); Clear(); }

    /*!
     * Change the semaphore to "set" state and tell any waiters in Wait() or
     * TimedWait() to resume.  Those threads are guaranteed to return from
     * Wait() or TimedWait() only if the semaphore is still "set" when they
     * actually do resume running.
     */
    void Set() { OS_MutexUnlock(&_impl); }

    /*!
     * Change the semaphore to "clear" state.
     */
    void Clear() { OS_MutexTryLock(&_impl); }

    /*!
     * Check whether the semaphore's state is "set".  This method always returns
     * immediately.
     *
     * @return  TRUE if the state is "set".
     */
    bool IsSet() { return !OS_MutexIsLocked(&_impl); }

    /*!
     * Block the calling thread until the semaphore's state is "set".  This
     * method returns immediately if the state is already "set".
     */
    void Wait() { OS_MutexLock(&_impl); OS_MutexUnlock(&_impl); }

    /*!
     * Block the calling thread until the semaphore's state is "set" or until
     * a timeout expires.  This method returns immediately if the state is
     * already "set".
     *
     *  @param[in] timeout  Maximum number of milliseconds to wait.
     *
     * @return  TRUE if the semaphore is in "set" state.  FALSE if this method
     *           returns instead due to the timeout.
     */
    bool TimedWait(unsigned timeout)
    {
        bool res = OS_MutexTimedLock(&_impl, timeout);
        if (res) OS_MutexUnlock(&_impl);
        return res;
    }

    OS_MUTEX_TYPE _impl;
} PINSYNC_POD_SEMAPHORE;

} // namespace

#endif // file guard
