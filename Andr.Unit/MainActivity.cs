//
// Copyright 2011-2012 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System.Reflection;
using Android.App;
using Android.OS;
using Android.NUnitLite;
using Android.NUnitLite.UI;
using NUnitLite.Runner;
using NUnit.Framework.Internal;

namespace Andr.Unit
{
	[Activity(Label = "Xamarin's Andr.Unit", MainLauncher = true)]
	public class MainActivity : RunnerActivity
	{
		protected override void OnCreate(Bundle bundle)
		{
			// tests can be inside the main assembly
			AddTest(Assembly.GetExecutingAssembly());

			Runner.Writer = new TcpTextWriter("10.0.1.2", 16384);
			Runner.TerminateAfterExecution = true;

			// you cannot add more assemblies once calling base
			base.OnCreate(bundle);
		}
	}
}