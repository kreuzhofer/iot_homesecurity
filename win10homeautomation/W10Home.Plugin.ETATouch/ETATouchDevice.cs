using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using W10Home.Core.Interfaces;
using Windows.Data.Xml.Dom;
using Windows.Web.Http;

namespace W10Home.Plugin.ETATouch
{
    public class ETATouchDevice : IDevice
    {
        private string _etatouchip;
		private List<TreeItem> _menustructure;
		private List<IChannel> _channels;

		public ETATouchDevice(string ETATouchIp)
        {
            _etatouchip = ETATouchIp;
        }

		public async Task InitializeAsync()
		{
			_menustructure = await GetMenuStructureFromEtaAsync();
			_channels = new List<IChannel>();

		}

		private void ParseChannelList(List<TreeItem> treeItems, List<IChannel> channels)
		{
			foreach (var item in treeItems)
			{
				if(item.SubItems == null)
				{
					ParseChannelList(item.SubItems, channels);
				}
				else
				{
					channels.Add(new EtaChannel(item.Name));
				}
			}
		}

        public async Task<List<TreeItem>> GetMenuStructureFromEtaAsync()
        {
            HttpClient client = new HttpClient();
            var content = (await client.GetStringAsync(new Uri($"http://{_etatouchip}:8080/user/menu"))).DecodeFromUtf8();
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
			var content = (await client.GetStringAsync(new Uri($"http://{_etatouchip}:8080/user/var{uri}"))).DecodeFromUtf8();
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

		public async Task<IEnumerable<IChannel>> GetChannelsAsync()
		{
			return _channels;
		}
	}

    public class TreeItem
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public List<TreeItem> SubItems { get; set; }
    }

	public class EtaValue
	{
		public int AdvTextOffset { get; set; }
		public int ScaleFactor { get; set; }
		public int DecPlaces { get; set; }
		public string Unit { get; set; }
		public string StrValue { get; set; }
		public int Value { get; set; }
	}

    public static class StringExtensions
    {
        public static string DecodeFromUtf8(this string utf8String)
        {
            // copy the string as UTF-8 bytes.
            byte[] utf8Bytes = new byte[utf8String.Length];
            for (int i = 0; i < utf8String.Length; ++i)
            {
                //Debug.Assert( 0 <= utf8String[i] && utf8String[i] <= 255, "the char must be in byte's range");
                utf8Bytes[i] = (byte)utf8String[i];
            }

            return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
        }
    }

	public class EtaChannel : IChannel
	{
		private string _name;

		public EtaChannel(string name)
		{
			_name = name;
		}

		public bool IsRead => true;

		public bool IsWrite => false;

		public string Name => _name;

		public Task<bool> SendMessageAsync(string messageBody)
		{
			throw new NotImplementedException();
		}
	}
}
