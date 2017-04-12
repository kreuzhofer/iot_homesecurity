namespace W10Home.Plugin.ABUS.SecVest.Utils
{
	public static class Base64
	{
		public static string EncodeTo64(string toEncode)
		{
			byte[] toEncodeAsBytes
		  = System.Text.Encoding.ASCII.GetBytes(toEncode);
			string returnValue
		  = System.Convert.ToBase64String(toEncodeAsBytes);
			return returnValue;
		}

		public static string DecodeFrom64(string encodedData)
		{
			byte[] encodedDataAsBytes
		  = System.Convert.FromBase64String(encodedData);
			string returnValue =
		    System.Text.Encoding.ASCII.GetString(encodedDataAsBytes);
			return returnValue;
		}
	}
}
