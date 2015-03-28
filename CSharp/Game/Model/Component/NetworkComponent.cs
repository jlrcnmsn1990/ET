﻿using System;
using Common.Base;
using Common.Network;
using TNet;
using UNet;

namespace Model
{
	public class NetworkComponent: Component<World>, IUpdate, IStart
	{
		private IService service;

		private void Accept(string host, int port, NetworkProtocol protocol = NetworkProtocol.TCP)
		{
			switch (protocol)
			{
				case NetworkProtocol.TCP:
					this.service = new TService(host, port);
					break;
				case NetworkProtocol.UDP:
					this.service = new UService(host, port);
					break;
				default:
					throw new ArgumentOutOfRangeException("protocol");
			}

			this.AcceptChannel();
		}

		public void Start()
		{
			this.Accept(World.Instance.Options.Host, World.Instance.Options.Port,
					World.Instance.Options.Protocol);
		}

		public void Update()
		{
			this.service.Update();
		}

		/// <summary>
		/// 接收连接
		/// </summary>
		private async void AcceptChannel()
		{
			while (true)
			{
				AChannel channel = await this.service.GetChannel();
				ProcessChannel(channel);
			}
		}

		/// <summary>
		/// 接收分发封包
		/// </summary>
		/// <param name="channel"></param>
		private static async void ProcessChannel(AChannel channel)
		{
			while (true)
			{
				byte[] message = await channel.RecvAsync();
				Env env = new Env();
				env[EnvKey.Channel] = channel;
				env[EnvKey.Message] = message;
				int opcode = BitConverter.ToUInt16(message, 0);
				// 这个区间表示消息是rpc响应消息
				if (opcode >= 40000 && opcode < 50000)
				{
					int id = BitConverter.ToInt32(message, 2);
					channel.RequestCallback(id, message, true);
					continue;
				}

				// 进行消息解析分发
#pragma warning disable 4014
				World.Instance.GetComponent<EventComponent<EventAttribute>>()
						.RunAsync(EventType.Message, env);
#pragma warning restore 4014
			}
		}
	}
}