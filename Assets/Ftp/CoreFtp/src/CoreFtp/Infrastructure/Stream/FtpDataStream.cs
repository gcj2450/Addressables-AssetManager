﻿namespace CoreFtp.Infrastructure.Stream
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public class FtpDataStream : Stream
    {
        private readonly Stream encapsulatedStream;
        private readonly FtpClient client;

        public override bool CanRead => encapsulatedStream.CanRead;
        public override bool CanSeek => encapsulatedStream.CanSeek;
        public override bool CanWrite => encapsulatedStream.CanWrite;
        public override long Length => encapsulatedStream.Length;

        public override long Position
        {
            get { return encapsulatedStream.Position; }
            set { encapsulatedStream.Position = value; }
        }


        public FtpDataStream(Stream encapsulatedStream, FtpClient client)
        {
            Debug.Log("[FtpDataStream] Constructing");
            this.encapsulatedStream = encapsulatedStream;
            this.client = client;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Log("[FtpDataStream] Disposing");
            base.Dispose(disposing);

            try
            {
                encapsulatedStream.Dispose();

                if (client.Configuration.DisconnectTimeoutMilliseconds.HasValue)
                {
                    client.ControlStream.SetTimeouts(client.Configuration.DisconnectTimeoutMilliseconds.Value);
                }
                client.CloseFileDataStreamAsync().Wait();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + " Closing the data stream took longer than expected");
            }
            finally
            {
                client.ControlStream.ResetTimeouts();
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[FtpDataStream] FlushAsync");
            await encapsulatedStream.FlushAsync(cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Debug.Log("[FtpDataStream] ReadAsync");
            return await encapsulatedStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Debug.Log("[FtpDataStream] WriteAsync");
            await encapsulatedStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
            Debug.Log("[FtpDataStream] Flush");
            encapsulatedStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Debug.Log("[FtpDataStream] Read");
            return encapsulatedStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Debug.Log("[FtpDataStream] Seek");
            return encapsulatedStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Debug.Log("[FtpDataStream] SetLength");
            encapsulatedStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Debug.Log("[FtpDataStream] Write");
            encapsulatedStream.Write(buffer, offset, count);
        }
    }
}
