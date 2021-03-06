//
// CurlResponseStream.cs
//
// Author:
//   Aaron Bockover <aaron@abock.org>
//
// Copyright 2014 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;

namespace Curl
{
	internal class CurlResponseStream : Stream
	{
		Multi multi;
		long length;
		long position;
		byte [] buffer = new byte [4096];
		int bufferWritePosition;
		int bufferLength;
		bool haveData;

		public CurlResponseStream (Easy easy) : this (new Multi { easy }, easy)
		{
		}

		public CurlResponseStream (Multi multi, Easy easy)
		{
			if (multi == null)
				throw new ArgumentNullException ("multi");
			else if (easy == null)
				throw new ArgumentNullException ("easy");

			this.multi = multi;
			easy.WriteHandler = AppendBuffer;
		}

		void CheckDisposed ()
		{
			if (multi == null)
				throw new ObjectDisposedException ("CurlResponseStream");
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (disposing && multi != null) {
				multi.Dispose ();
				multi = null;
			}
		}

		void AppendBuffer (byte [] data)
		{
			CheckDisposed ();

			var requiredSize = bufferWritePosition + data.Length;
			if (requiredSize > buffer.Length) {
				var newSize = buffer.Length;
				while (newSize < requiredSize)
					newSize <<= 1;
				Array.Resize (ref buffer, newSize);
			}

			Array.Copy (data, 0, buffer, bufferWritePosition, data.Length);

			bufferWritePosition += data.Length;
			bufferLength += data.Length;
			length += data.Length;

			haveData = true;
		}

		public override int Read (byte [] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");
			else if (offset < 0 || offset >= buffer.Length)
				throw new ArgumentOutOfRangeException ("offset");
			else if (count < 0)
				throw new ArgumentOutOfRangeException ("count");
			else if (offset + count > buffer.Length)
				throw new ArgumentException ("sum of offset and count is larger than the buffer length");

			CheckDisposed ();

			while (!haveData || (bufferLength == 0 && multi.HandlesRemaining > 0))
				multi.AutoPerformWithSelect ();

			count = Math.Min (bufferLength, count);
			if (count == 0)
				return 0;

			Array.Copy (this.buffer, 0, buffer, offset, count);

			if (bufferWritePosition - count > 0)
				Array.Copy (this.buffer, count, this.buffer, 0, bufferWritePosition - count);

			bufferWritePosition -= count;
			bufferLength -= count;
			position += count;

			return count;
		}

		public override long Length {
			get { return length; }
		}

		public override long Position {
			get { return position; }
			set { throw new NotImplementedException (); }
		}

		public override bool CanRead {
			get { return true; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return false; }
		}

		public override void Flush ()
		{
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException ();
		}
	}
}