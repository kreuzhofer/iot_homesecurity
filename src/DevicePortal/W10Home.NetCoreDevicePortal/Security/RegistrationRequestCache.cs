using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace W10Home.DevicePortal.Security
{
	public class RegistrationRequest
	{
		public string Pin { get; set; }
		public string DeviceId { get; set; }
		public DateTime ValidUntil { get; set; }
	}

	public static class RegistrationRequestCache
	{
		private static readonly List<RegistrationRequest> _registrations = new List<RegistrationRequest>();

		public static RegistrationRequest GetNewPin(string deviceId)
		{
			var rand = new Random();
			string pin; // six pin digit code
			do
			{
				pin = rand.Next(100000, 999999).ToString();
			} while (_registrations.Any(p=>p.Pin == pin));

			var reg = _registrations.SingleOrDefault(r => r.DeviceId == deviceId); // existing registration -> replace
			if (reg != null)
			{
				_registrations.Remove(reg);
			}

			var resultReg = new RegistrationRequest()
			{
				DeviceId = deviceId,
				Pin = pin,
				ValidUntil = DateTime.Now.AddMinutes(1)
			};
			_registrations.Add(resultReg);
			new Timer(r =>
			{
				if (_registrations.Contains(r))
				{
					_registrations.Remove((RegistrationRequest) r);
				}
			}, resultReg, 60*1000, 0);
			return resultReg;
		}

		public static bool ValidatePin(string pin, out string deviceId)
		{
			var reg = _registrations.SingleOrDefault(r => r.Pin == pin);
			if (reg != null && reg.Pin == pin && reg.ValidUntil<=DateTime.Now)
			{
				_registrations.Remove(reg);
				deviceId = reg.DeviceId;
				return true;
			}
			deviceId = null;
			return false;
		}

	}
}