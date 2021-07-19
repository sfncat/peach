using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Managed.Adb.Logs {
	public class LogReceiver {
		private const int ENTRY_HEADER_SIZE = 20; // 2*2 + 4*4; see LogEntry.
		public LogReceiver ( ILogListener listener ) {
			EntryDataOffset = 0;
			EntryHeaderBuffer = new byte[ENTRY_HEADER_SIZE];
			EntryHeaderOffset = 0;
			Listener = listener;
		}

		private int EntryDataBegin { get; set; }
		private int EntryDataOffset { get; set; }
		private int EntryHeaderOffset { get; set; }
		private byte[] EntryHeaderBuffer { get; set; }

		private LogEntry CurrentEntry { get; set; }
		private ILogListener Listener { get; set; }
		public bool IsCancelled { get; private set; }

		public void Cancel ( ) {
			this.IsCancelled = true;
		}

		public void ParseNewData ( byte[] data, int offset, int length ) {
			// notify the listener of new raw data
			if ( Listener != null ) {
				Listener.NewData ( data, offset, length );
			}

			// loop while there is still data to be read and the receiver has not be cancelled.
			while ( length > 0 && !IsCancelled ) {
				// first check if we have no current entry.
				if ( CurrentEntry == null ) {
					if ( EntryHeaderOffset + length < ENTRY_HEADER_SIZE ) {
						// if we don't have enough data to finish the header, save
						// the data we have and return
						Array.Copy ( data, offset, EntryHeaderBuffer, EntryHeaderOffset, length );
						EntryHeaderOffset += length;
						return;
					} else {
						// we have enough to fill the header, let's do it.
						// did we store some part at the beginning of the header?
						if ( EntryHeaderOffset != 0 ) {
							// copy the rest of the entry header into the header buffer
							int size = ENTRY_HEADER_SIZE - EntryHeaderOffset;
							Array.Copy ( data, offset, EntryHeaderBuffer, EntryHeaderOffset, size );

							// create the entry from the header buffer
							CurrentEntry = CreateEntry ( EntryHeaderBuffer, 0 );

							// since we used the whole entry header buffer, we reset  the offset
							EntryHeaderOffset = 0;

							// adjust current offset and remaining length to the beginning
							// of the entry data
							offset += size;
							length -= size;
						} else {
							// create the entry directly from the data array
							CurrentEntry = CreateEntry ( data, offset );
							// adjust current offset and remaining length to the beginning
							// of the entry data
							offset += ENTRY_HEADER_SIZE;
							length -= ENTRY_HEADER_SIZE;
						}

						EntryDataBegin = CurrentEntry.HeaderSize == 0 ? 0 : CurrentEntry.HeaderSize - ENTRY_HEADER_SIZE;
					}
				}

				while (EntryDataBegin > 0 && length > 0) {
					var eat = Math.Min(EntryDataBegin, length);

					EntryDataBegin -= eat;
					length -= eat;
					offset += eat;
				}

				// at this point, we have an entry, and offset/length have been updated to skip
				// the entry header.
				if ( length >= CurrentEntry.Length - EntryDataOffset ) {
					// compute and save the size of the data that we have to read for this entry,
					// based on how much we may already have read.
					int dataSize = CurrentEntry.Length - EntryDataOffset;

					// we only read what we need, and put it in the entry buffer.
					Array.Copy ( data, offset, CurrentEntry.Data, EntryDataOffset, dataSize );

					// notify the listener of a new entry
					if ( Listener != null ) {
						Listener.NewEntry ( CurrentEntry );
					}

					// reset some flags: we have read 0 data of the current entry.
					// and we have no current entry being read.
					EntryDataOffset = 0;
					CurrentEntry = null;

					// and update the data buffer info to the end of the current entry / start
					// of the next one.
					offset += dataSize;
					length -= dataSize;
				} else {
					// we don't have enough data to fill this entry, so we store what we have
					// in the entry itself.
					Array.Copy ( data, offset, CurrentEntry.Data, EntryDataOffset, length );

					// save the amount read for the data.
					EntryDataOffset += length;
					return;
				}
			}
		}

		DateTime DateTimeFromTimeT(int sec)
		{
			// http://msdn.microsoft.com/en-us/library/ms724228
			long fileTime = (((long)sec) * 10000000) + 116444736000000000;
			return DateTime.FromFileTimeUtc(fileTime);
		}

		private LogEntry CreateEntry ( byte[] data, int offset ) {
			if ( data.Length < offset + ENTRY_HEADER_SIZE ) {
				throw new ArgumentException ( "Buffer not big enough to hold full LoggerEntry header" );
			}

			// create the new entry and fill it.
			LogEntry entry = new LogEntry ( );
			entry.Length = data.SwapU16bitFromArray ( offset );

			offset += 2;
			entry.HeaderSize = data.SwapU16bitFromArray(offset);
			offset += 2;

			entry.ProcessId = data.Swap32bitFromArray ( offset );
			offset += 4;
			entry.ThreadId = data.Swap32bitFromArray ( offset );
			offset += 4;
			var sec = data.Swap32bitFromArray ( offset );

			offset += 4;
			entry.NanoSeconds = data.Swap32bitFromArray ( offset );
			offset += 4;

			entry.TimeStamp = DateTimeFromTimeT(sec);

			// allocate the data
			entry.Data = new byte[entry.Length];

			return entry;
		}
	}
}
