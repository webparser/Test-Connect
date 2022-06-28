using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iFlow.DataProtocols
{
	internal abstract class AbstractResponsePacket
	{
		public ushort RegAddress;

		public int Offset;
		public int Length;
	}

	internal class ResponseDataPacket : AbstractResponsePacket
	{
		public ushort RegLength { get { return (ushort)(DataLength / 2); } }

		public byte[] Data;
		public int DataOffset;
		public int DataLength;

		public override string ToString()
		{
			return $"{{ResponseDataPacket: RegAddress = {RegAddress}, RegLength = {RegLength}}}";
		}
	}

	internal class ResponseErrorPacket : AbstractResponsePacket
	{
		public byte ErrorCode;
		public string Error;
		public override string ToString()
		{
			return $"{{ResponseErrorPacket: RegAddress = {RegAddress}, ErrorCode = {ErrorCode}}}";
		}
	}

	internal class Response
	{
		public List<AbstractResponsePacket> Packets = new List<AbstractResponsePacket>();
	}

}
