using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public interface IAbstractDataSource
	{
		GlobalTags GlobalTags { get; }
		int[] TagIds { get; }
	}

	public abstract class AbstractDataSource<TConfig, TTagAddress> : IDataSource, IAbstractDataSource
		where TConfig : new()
	{
		public AbstractDataSource(GlobalTags globalTags)
			: base()
		{
			GlobalTags = globalTags;
		}

		public virtual void Dispose()
		{
			realtimeData?.Dispose();
		}

		public GlobalTags GlobalTags { get; }
		public IdDictionary<TTagAddress> TagAddresses { get; private set; } = new IdDictionary<TTagAddress>();

		public event ExceptionHandler ExceptionEvent;

		public int[] TagIds
		{
			get { return TagAddresses.Keys.ToArray(); }
		}

		public IRealtimeData RealtimeData
		{
			get
			{
				if (realtimeData == null)
				{
					realtimeData = CreateRealtimeData();
					realtimeData.ExceptionEvent += Data_ExceptionEvent;
				}
				return realtimeData;
			}
		}
		private IAbstractRealtimeData realtimeData;

		protected virtual IAbstractRealtimeData CreateRealtimeData()
		{
			throw new NotImplementedException();
		}

		public IHistoricalData HistoricalData
		{
			get
			{
				if (historicalData == null)
				{
					historicalData = CreateHistoricalData();
					historicalData.ExceptionEvent += Data_ExceptionEvent;
				}
				return historicalData;
			}
		}
		private IAbstractHistoricalData historicalData;

		protected virtual IAbstractHistoricalData CreateHistoricalData()
		{
			throw new NotImplementedException();
		}

		public IMetaData MetaData
		{
			get
			{
				if (metaData == null)
				{
					metaData = CreateMetaData();
					metaData.ExceptionEvent += Data_ExceptionEvent;
				}
				return metaData;
			}
		}
		private IAbstractMetaData metaData;

		protected virtual IAbstractMetaData CreateMetaData()
		{
			throw new NotImplementedException();
		}

		public virtual void LoadConfig(TConfig config)
		{
			Dictionary<string, TTagAddress> configTagAddresses = LoadTagAdresses(config);
			if (configTagAddresses != null)
			{
				configTagAddresses
					.Where(x => !GlobalTags.TryGet(x.Key, out Tag tag, out string error))
					.ThrowExceptionIfAny(x => $"В глобальном списке тэгов не найдено соответствие для тэг-маппинга {x.Key}-{x.Value}.");

				IdDictionary<TTagAddress> tagAddresses = configTagAddresses
					.ToIdDictionary(x => GlobalTags[x.Key].Id, x => x.Value);
				ValidateTagAddresses(tagAddresses);
				TagAddresses = tagAddresses;
			}
		}

		protected abstract Dictionary<string, TTagAddress> LoadTagAdresses(TConfig config);

		/// <summary>
		/// Проверка, что тэг-маппинг не null, не пуст, не дублируются параметры Id и Address
		/// </summary>
		/// <param name="tagMappings"></param>
		protected virtual void ValidateTagAddresses(IdDictionary<TTagAddress> tagAddresses)
		{
			//tagAddresses
			//	.Where(x => string.IsNullOrWhiteSpace(x.Value)).ToArray();
			//if (invalidTags.Any())
			//	throw new MultiException(invalidTags.Select(x => $"В тэг-маппинге {x.Value} не указан параметр Address."));

			//invalidTags = tags.Where(x => tags.Any(y => x.Value != y.Value && IgnoreCase.Equals(x.Value.Address, y.Value.Address, true))).ToArray();
			//if (invalidTags.Any())
			//	throw new MultiException(invalidTags.Select(x => $"В тэг-маппинге {x.Value} дублируется параметр Address."));
		}

		protected virtual void OnException(Exception ex, string comments = null)
		{
			ExceptionEvent?.Invoke(this, ex, comments);
		}

		protected virtual void Data_ExceptionEvent(object sender, Exception ex, string comments = null)
		{
			comments = (sender == RealtimeData
				? "Ошибка в объекте RealtimeData."
				: sender == HistoricalData
					? "Ошибка в объекте HistoricalData."
					: sender == MetaData
						? "Ошибка в объекте MetaData."
						: "Ошибка приложения.") + " " + (comments ?? "");
			OnException(ex, comments);
		}

	}

}