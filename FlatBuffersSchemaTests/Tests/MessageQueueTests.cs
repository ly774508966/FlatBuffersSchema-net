﻿/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Wu Yuntao
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
*/

using NUnit.Framework;
using System;

namespace FlatBuffers.Schema.Tests
{
	public class MessageQueueTests
	{
		private MessageSchema schema;
		private MessageQueue queue;

		public MessageQueueTests()
		{
			schema = new MessageSchema();
			queue = new MessageQueue(schema);
		}

		[Test]
		public void TestPingMessage()
		{
			// Register message creators
			schema.Register(MessageIds.Ping, bb => PingMessage.GetRootAsPingMessage(bb));
			schema.Register(MessageIds.Pong, bb => PongMessage.GetRootAsPongMessage(bb));

			var count = 10;
			var msg = "TestPing10";
			var lists = new int[][][]
			{
				new int[][]
				{
					new int[] { 1, 2 },
					new int[] { 2, 3 },
				},
				new int[][]
				{
					new int[] { 3, 4 },
					new int[] { 4, 5 },
					new int[] { 5, 6 },
				},
				new int[][]
				{
				},

			};
			var ping = CreatePingMessage(count, msg, lists);
			queue.Enqueue(ping.ToProtocolMessage(MessageIds.Ping));

			var message = queue.Dequeue();
			Assert.AreEqual((int)MessageIds.Ping, message.Id);

			Assert.IsTrue(message.Body is PingMessage);
			var pingBody = (PingMessage)message.Body;
			Assert.AreEqual(count, pingBody.Count);
			Assert.AreEqual(msg, pingBody.Msg);

			Assert.AreEqual(lists.Length, pingBody.ListsLength);

			Assert.AreEqual(lists[0].Length, pingBody.Lists(0).Value.ItemsLength);
			Assert.AreEqual(lists[0][0][0], pingBody.Lists(0).Value.Items(0).Value.Key);
			Assert.AreEqual(lists[0][0][1], pingBody.Lists(0).Value.Items(0).Value.Value);

			Assert.AreEqual(lists[1].Length, pingBody.Lists(1).Value.ItemsLength);
			Assert.AreEqual(lists[1][2][0], pingBody.Lists(1).Value.Items(2).Value.Key);
			Assert.AreEqual(lists[1][2][1], pingBody.Lists(1).Value.Items(2).Value.Value);

			Assert.AreEqual(lists[2].Length, pingBody.Lists(2).Value.ItemsLength);
		}

		static FlatBufferBuilder CreatePingMessage(int count, string msg, int[][][] lists)
		{
			var fbb = new FlatBufferBuilder(1024);

			var oLists = new Offset<PingList>[lists.Length];
			for (int i = 0; i < lists.Length; i++)
			{
				var list = lists[i];

				var oItems = new Offset<PingListItem>[list.Length];
				for (int j = 0; j < list.Length; j++)
				{
					var item = list[j];

					oItems[j] = PingListItem.CreatePingListItem(fbb, item[0], item[1]);
				}

				var voItems = PingList.CreateItemsVector(fbb, oItems);
				oLists[i] = PingList.CreatePingList(fbb, i, voItems);
			}

			var voLists = PingMessage.CreateListsVector(fbb, oLists);
			var oMsg = fbb.CreateString(msg);
			var oPing = PingMessage.CreatePingMessage(fbb, count, oMsg, voLists);
			PingMessage.FinishPingMessageBuffer(fbb, oPing);

			return fbb;
		}
	}
}