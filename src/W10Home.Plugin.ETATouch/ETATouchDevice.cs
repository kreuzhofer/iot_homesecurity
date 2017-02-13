using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using W10Home.Core.Channels;
using W10Home.Core.Configuration;
using W10Home.Interfaces;
using Windows.Data.Xml.Dom;

namespace W10Home.Plugin.ETATouch
{
	public class ETATouchDevice : IDevice
    {
        private string _etatouchUrl;
		private List<TreeItem> _menustructure;
		private List<IChannel> _channels;

		public async Task InitializeAsync(IDeviceConfiguration configuration)
		{
			_etatouchUrl = configuration.Properties["ConnectionString"];
			_menustructure = await GetMenuStructureFromEtaAsync();
			_channels = new List<IChannel>();
			await ParseChannelListAsync(_menustructure, _channels);
		}

		private async Task ParseChannelListAsync(List<TreeItem> treeItems, List<IChannel> channels)
		{
			foreach (var item in treeItems)
			{
				if (item.SubItems != null)
				{
					await ParseChannelListAsync(item.SubItems, channels);
				}
				else
				{
					var value = await GetValueFromEtaUriAsync(item.Uri);

                    ChannelType channelType = ChannelType.None;
                    UnitType unitType = UnitType.DegreesCelsius;
					if(value.Unit == "°C")
					{
                        channelType = ChannelType.Temperature;
                    }
					channels.Add(new EtaChannel(item.Name, channelType, unitType));
				}
			}
		}

		public async Task<List<TreeItem>> GetMenuStructureFromEtaAsync()
        {
            HttpClient client = new HttpClient();
            var content = (await client.GetStringAsync(new Uri($"{_etatouchUrl}/user/menu")));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content, new XmlLoadSettings() { ElementContentWhiteSpace = false });

            var result = new List<TreeItem>();

            ParseTree(doc.DocumentElement.ChildNodes.Single(d => d.NodeName == "menu"), result);
            return result;
        }

		public async Task<EtaValue> GetValueFromEtaValuePathAsync(List<TreeItem> menu, string valuePath)
		{
			string[] pathElements = valuePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			var currentItems = menu;
			TreeItem menuItem = null;
			foreach (var pathElement in pathElements)
			{
				menuItem = currentItems.Single(m => m.Name == pathElement);
				currentItems = menuItem.SubItems;
			}
			return await GetValueFromEtaUriAsync(menuItem.Uri);
		}

		public async Task<EtaValue> GetValueFromEtaUriAsync(string uri)
		{
			HttpClient client = new HttpClient();
			var content = (await client.GetStringAsync(new Uri($"{_etatouchUrl}/user/var{uri}")));
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(content, new XmlLoadSettings() { ElementContentWhiteSpace = false });
			var valueNode = doc.DocumentElement.ChildNodes.Single(d => d.NodeName == "value");
			var result = new EtaValue()
			{
				AdvTextOffset = int.Parse((string)valueNode.Attributes.Single(a => a.NodeName == "advTextOffset").NodeValue),
				ScaleFactor = int.Parse((string)valueNode.Attributes.Single(a => a.NodeName == "scaleFactor").NodeValue),
				DecPlaces = int.Parse((string)valueNode.Attributes.Single(a => a.NodeName == "decPlaces").NodeValue),
				Unit = (string)valueNode.Attributes.Single(a => a.NodeName == "unit").NodeValue,
				StrValue = (string)valueNode.Attributes.Single(a => a.NodeName == "strValue").NodeValue,
				Value = int.Parse((string)valueNode.InnerText)
			};
			return result;
		}

		private static void ParseTree(IXmlNode root, List<TreeItem> result)
		{
			var menuRoot = root.ChildNodes.Where(n => n.NodeType == NodeType.ElementNode);
			foreach (var fub in menuRoot)
			{
				var uri = fub.Attributes.Single(a => a.NodeName == "uri").NodeValue;
				var name = fub.Attributes.Single(a => a.NodeName == "name").NodeValue;
				var treeItem = new TreeItem
				{
					Name = name.ToString(),
					Uri = uri.ToString()
				};
				result.Add(treeItem);
				if (fub.ChildNodes.Any(c => c.NodeType == NodeType.ElementNode))
				{
					treeItem.SubItems = new List<TreeItem>();
					ParseTree(fub, treeItem.SubItems);
				}
			}
		}

		public Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			return Task.FromResult(_channels.AsEnumerable());
		}

	    public async Task Teardown()
	    {
		    // nothing to do yet
	    }
    }
}
