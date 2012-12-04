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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

using Android.App;
using Android.Content;
using Android.Widget;

using NUnitLite;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.WorkItems;
using System.Reflection;
using System.Collections;
using Android.NUnitLite.UI;
using MonoDroid.Dialog;

namespace Android.NUnitLite
{
	public class AndroidRunner : ITestListener, ITestFilter
	{
		public int passed, failed, skipped, inconclusive;
		Options options;
		NUnitLiteTestAssemblyBuilder builder = new NUnitLiteTestAssemblyBuilder ();
		Dictionary<string, object> empty = new Dictionary<string, object> ();

		public List<Assembly> Assemblies = new List<Assembly> ();

		public AndroidRunner ()
		{
		}
		public bool TerminateAfterExecution { get; set; }
		
		public Options Options { 
			get {
				if (options == null)
					options = new Options ();
				return options;
			}
			set { options = value; }
		}

		public TestSuite LoadAssembly (string assemblyName, IDictionary settings)
		{
			return builder.Build (assemblyName, settings ?? empty);
		}

		public TestSuite LoadAssembly (Assembly assembly, IDictionary settings)
		{
			return builder.Build (assembly, settings ?? empty);
		}

		#region writer
		
		public TextWriter Writer { get; set; }
		
		public bool OpenWriter (string message, Context activity)
		{
			DateTime now = DateTime.Now;
			// let the application provide it's own TextWriter to ease automation with AutoStart property
			if (Writer == null) {
				if (Options.ShowUseNetworkLogger) {
					Console.WriteLine ("[{0}] Sending '{1}' results to {2}:{3}", now, message, Options.HostName, Options.HostPort);
					try {
						Writer = new TcpTextWriter (Options.HostName, Options.HostPort);
					} catch (SocketException) {
						string msg = String.Format ("Cannot connect to {0}:{1}. Start network service or disable network option", options.HostName, options.HostPort);
						Toast.MakeText (activity, msg, ToastLength.Long).Show ();
						return false;
					}
				} else {
					Writer = Console.Out;
				}
			}

			Writer.WriteLine ("[Runner executing:\t{0}]", message);

			// FIXME: provide valid MFA version
			Writer.WriteLine ("[M4A Version:\t{0}]", "???");
			
			Writer.WriteLine ("[Board:\t\t{0}]", Android.OS.Build.Board);
			Writer.WriteLine ("[Bootloader:\t{0}]", Android.OS.Build.Bootloader);
			Writer.WriteLine ("[Brand:\t\t{0}]", Android.OS.Build.Brand);
			Writer.WriteLine ("[CpuAbi:\t{0} {1}]", Android.OS.Build.CpuAbi, Android.OS.Build.CpuAbi2);
			Writer.WriteLine ("[Device:\t{0}]", Android.OS.Build.Device);
			Writer.WriteLine ("[Display:\t{0}]", Android.OS.Build.Display);
			Writer.WriteLine ("[Fingerprint:\t{0}]", Android.OS.Build.Fingerprint);
			Writer.WriteLine ("[Hardware:\t{0}]", Android.OS.Build.Hardware);
			Writer.WriteLine ("[Host:\t\t{0}]", Android.OS.Build.Host);
			Writer.WriteLine ("[Id:\t\t{0}]", Android.OS.Build.Id);
			Writer.WriteLine ("[Manufacturer:\t{0}]", Android.OS.Build.Manufacturer);
			Writer.WriteLine ("[Model:\t\t{0}]", Android.OS.Build.Model);
			Writer.WriteLine ("[Product:\t{0}]", Android.OS.Build.Product);
			Writer.WriteLine ("[Radio:\t\t{0}]", Android.OS.Build.Radio);
			Writer.WriteLine ("[Tags:\t\t{0}]", Android.OS.Build.Tags);
			Writer.WriteLine ("[Time:\t\t{0}]", Android.OS.Build.Time);
			Writer.WriteLine ("[Type:\t\t{0}]", Android.OS.Build.Type);
			Writer.WriteLine ("[User:\t\t{0}]", Android.OS.Build.User);
			Writer.WriteLine ("[VERSION.Codename:\t{0}]", Android.OS.Build.VERSION.Codename);
			Writer.WriteLine ("[VERSION.Incremental:\t{0}]", Android.OS.Build.VERSION.Incremental);
			Writer.WriteLine ("[VERSION.Release:\t{0}]", Android.OS.Build.VERSION.Release);
			Writer.WriteLine ("[VERSION.Sdk:\t\t{0}]", Android.OS.Build.VERSION.Sdk);
			Writer.WriteLine ("[VERSION.SdkInt:\t{0}]", Android.OS.Build.VERSION.SdkInt);
			Writer.WriteLine ("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output
			
			// FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, Linker options)

			passed = 0;
			failed = 0;
			skipped = 0;
		 	inconclusive = 0;

			return true;
		}
		
		public void CloseWriter ()
		{
			Writer.Close ();
			Writer = null;
		}

		#endregion
		
		public void TestStarted (ITest test)
		{
			if (test is TestSuite) {
				Writer.WriteLine ();
				time.Push (DateTime.UtcNow);
				Writer.WriteLine (test.Name);
			}
		}

		Stack<DateTime> time = new Stack<DateTime> ();
			
		public void TestFinished (ITestResult result)
		{
			AndroidRunner.Results [result.Test.FullName ?? result.Test.Name] = result as TestResult;
			
			if (result.Test is TestSuite) {
				//if (!result.IsError && !result.IsFailure && !result.IsSuccess && !result.Executed)
				//Writer.WriteLine ("\t[INFO] {0}", result.Message);
				if (result.ResultState.Status != TestStatus.Failed
					&& result.ResultState.Status != TestStatus.Skipped
					&& result.ResultState.Status != TestStatus.Passed
					&& result.ResultState.Status != TestStatus.Inconclusive)
					Writer.WriteLine ("\t[INFO] {0}", result.Message);
				
				var diff = DateTime.UtcNow - time.Pop ();
				Writer.WriteLine ("{0} : {1} ms", result.Test.Name, diff.TotalMilliseconds);

			} else {
				if (result.ResultState.Status == TestStatus.Passed) {
				Writer.Write ("\tTest Passed: ");
					passed++;
				} else if (result.ResultState.Status == TestStatus.Failed || result.ResultState.Label == "Error") {
					Writer.Write ("\tTest Failed: ");
					failed++;
				} else if (result.ResultState.Status == TestStatus.Skipped || result.ResultState.Label == "Ignored" || result.ResultState.Label == "Invalid"){
					Writer.Write ("\tTest Skipped: ");
					skipped++;
				} else if (result.ResultState.Status == TestStatus.Inconclusive) {
					Writer.Write ("\tTest Inconclusive: ");
					inconclusive++;
				}

				Writer.Write (result.Test.Name);

				string message = result.Message;
				if (!String.IsNullOrEmpty (message)) {
					Writer.Write (" : {0}", message.Replace ("\r\n", "\\r\\n"));
				}
			
				Writer.WriteLine ();

				string stacktrace = result.StackTrace;
				if (!String.IsNullOrEmpty (result.StackTrace)) {
					string[] lines = stacktrace.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string line in lines)
						Writer.WriteLine ("\t\t{0}", line);
				}
			}
		}
		
		static AndroidRunner runner = new AndroidRunner ();
		
		static public AndroidRunner Runner {
			get { return runner; }
		}
		
		static List<TestSuite> top = new List<TestSuite> ();
		static Dictionary<string,TestSuite> suites = new Dictionary<string, TestSuite> ();
		static Dictionary<string,TestResult> results = new Dictionary<string, TestResult> ();
		
		static public IList<TestSuite> AssemblyLevel {
			get { return top; }
		}
		
		static public IDictionary<string,TestSuite> Suites {
			get { return suites; }
		}
		
		static public IDictionary<string,TestResult> Results {
			get { return results; }
		}

		public TestResult Run (NUnit.Framework.Internal.Test test)
		{
			TestExecutionContext current = TestExecutionContext.CurrentContext;
			current.WorkDirectory = Environment.CurrentDirectory;
			current.Listener = this;
			current.TestObject = test is TestSuite ? null : Reflect.Construct ((test as TestMethod).Method.ReflectedType, null);
			WorkItem wi = WorkItem.CreateWorkItem (test, current, this);
			wi.Execute ();
			return wi.Result;
		}

		public void TestOutput (TestOutput testOutput)
		{
			//TODO: Not yet implemented
		}

		public bool Pass (ITest pass)
		{
			return true;
		}
	}
}