using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	internal static class QualityExt
	{
		public static Quality Convert(this Opc.Da.Quality quality)
		{
			switch (quality.QualityBits)
			{
				case Opc.Da.qualityBits.good: return Quality.Good;
				case Opc.Da.qualityBits.goodLocalOverride: return Quality.GoodLocalOverride;
				case Opc.Da.qualityBits.bad: return Quality.Bad;
				case Opc.Da.qualityBits.badConfigurationError: return Quality.BadConfigurationError;
				case Opc.Da.qualityBits.badNotConnected: return Quality.BadNotConnected;
				case Opc.Da.qualityBits.badDeviceFailure: return Quality.BadDeviceFailure;
				case Opc.Da.qualityBits.badSensorFailure: return Quality.BadSensorFailure;
				case Opc.Da.qualityBits.badLastKnownValue: return Quality.BadLastKnownValue;
				case Opc.Da.qualityBits.badCommFailure: return Quality.BadCommFailure;
				case Opc.Da.qualityBits.badOutOfService: return Quality.BadOutOfService;
				case Opc.Da.qualityBits.badWaitingForInitialData: return Quality.BadWaitingForInitialData;
				case Opc.Da.qualityBits.uncertain: return Quality.Uncertain;
				case Opc.Da.qualityBits.uncertainLastUsableValue: return Quality.UncertainLastUsableValue;
				case Opc.Da.qualityBits.uncertainSensorNotAccurate: return Quality.UncertainSensorNotAccurate;
				case Opc.Da.qualityBits.uncertainEUExceeded: return Quality.UncertainEUExceeded;
				case Opc.Da.qualityBits.uncertainSubNormal: return Quality.UncertainSubNormal;
				default: throw new WrongTypeException(quality);
			}
		}


	}
}
