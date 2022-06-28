using iFlow.Interfaces;
using iFlow.Utils;
using Opc;

namespace iFlow.DataProtocols
{
	internal static class OperationResultExt
	{
		public static OperationResult Convert(this ResultID resultId)
		{
			if (resultId == ResultID.S_OK)
				return OperationResult.Ok;
			if (resultId == ResultID.S_FALSE)
				return OperationResult.False;
			if (resultId == ResultID.E_ACCESS_DENIED)
				return OperationResult.AccessDenied;
			if (resultId == ResultID.E_FAIL)
				return OperationResult.Fail;
			if (resultId == ResultID.E_INVALIDARG)
				return OperationResult.InvalidArg;
			if (resultId == ResultID.E_NETWORK_ERROR)
				return OperationResult.NetworkError;
			if (resultId == ResultID.E_NOTSUPPORTED)
				return OperationResult.NotSupported;
			if (resultId == ResultID.E_OUTOFMEMORY)
				return OperationResult.OutOfMemory;
			if (resultId == ResultID.E_TIMEDOUT)
				return OperationResult.Timedout;

			if (resultId == ResultID.Da.E_BADTYPE)
				return OperationResult.BadType;
			if (resultId == ResultID.Da.E_INVALIDCONTINUATIONPOINT)
				return OperationResult.InvalidContinuationPoint;
			if (resultId == ResultID.Da.E_INVALIDHANDLE)
				return OperationResult.InvalidHandle;
			if (resultId == ResultID.Da.E_INVALID_FILTER)
				return OperationResult.InvalidFilter;
			if (resultId == ResultID.Da.E_INVALID_ITEM_NAME)
				return OperationResult.InvalidItemName;
			if (resultId == ResultID.Da.E_INVALID_ITEM_PATH)
				return OperationResult.InvalidItemPath;
			if (resultId == ResultID.Da.E_INVALID_PID)
				return OperationResult.InvalidPid;
			if (resultId == ResultID.Da.E_NO_ITEM_BUFFERING)
				return OperationResult.NoItemBuffering;
			if (resultId == ResultID.Da.E_NO_ITEM_DEADBAND)
				return OperationResult.NoItemDeadband;
			if (resultId == ResultID.Da.E_NO_ITEM_SAMPLING)
				return OperationResult.NoItemSampling;
			if (resultId == ResultID.Da.E_NO_WRITEQT)
				return OperationResult.NoWriteQt;
			if (resultId == ResultID.Da.E_RANGE)
				return OperationResult.Range;
			if (resultId == ResultID.Da.E_READONLY)
				return OperationResult.ReadOnly;
			if (resultId == ResultID.Da.E_UNKNOWN_ITEM_NAME)
				return OperationResult.UnknownItemName;
			if (resultId == ResultID.Da.E_UNKNOWN_ITEM_PATH)
				return OperationResult.UnknownItemPath;
			if (resultId == ResultID.Da.E_WRITEONLY)
				return OperationResult.WriteOnly;
			if (resultId == ResultID.Da.S_CLAMP)
				return OperationResult.Clamp;
			if (resultId == ResultID.Da.S_DATAQUEUEOVERFLOW)
				return OperationResult.DataQueueOverflow;
			if (resultId == ResultID.Da.S_UNSUPPORTEDRATE)
				return OperationResult.UnsupportedRate;
			throw new WrongTypeException(resultId);
		}
	}

}
