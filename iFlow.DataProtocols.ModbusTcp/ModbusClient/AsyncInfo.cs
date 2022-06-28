using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace iFlow.DataProtocols
{
	internal abstract class AbstractAsyncInfo : IDisposable
	{
		public AbstractAsyncInfo(Socket socket) : base()
		{
			Socket = socket;
		}

		public void Dispose()
		{
			Event.Set();
			Event.Close();
		}

		public readonly Socket Socket;
		public readonly ManualResetEvent Event = new ManualResetEvent(false);
		public Exception Exception;
	}

	internal class ConnectAsyncInfo : AbstractAsyncInfo
	{
		public ConnectAsyncInfo(Socket socket) : base(socket)
		{
		}
	}

	internal class RequestAsyncInfo : AbstractAsyncInfo
	{
		public RequestAsyncInfo(Socket socket, int byteCount) : base(socket)
		{
			ByteCount = byteCount;
		}

		public readonly int ByteCount;
	}


	internal class ResponseAsyncInfo : AbstractAsyncInfo
	{
		public ResponseAsyncInfo(Socket socket, int packetCount, Response response) : base(socket)
		{
			RequestPacketCount = packetCount;
			Response = response;
		}

		public readonly int RequestPacketCount;
		public readonly byte[] ReceiveBuffer = new byte[1024];
		public readonly Response Response;
		public byte[] ParseBuffer = new byte[0];
	}

}
