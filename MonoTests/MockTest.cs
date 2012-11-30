using System;

using NUnit.Framework;
using Android.NUnitLite;

namespace MonoTests
{
	[TestFixture]
	public class MockTest
	{
		public MockTest ()
		{
		}

		[Test, Description ("Pass a Test")]
		public void Pass () 
		{
			Assert.IsTrue (true);
		}
	}
}

